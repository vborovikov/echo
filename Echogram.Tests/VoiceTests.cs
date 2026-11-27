namespace Echogram.Tests;

using System.Net;
using System.Text;
using Echo;
using Echo.Telegram;
using Microsoft.Extensions.Logging.Abstractions;

[TestClass]
public sealed class VoiceTests
{
    [TestMethod]
    public async Task SendVoice_WithFileId_SendsJson()
    {
        var (bot, handler) = TestBot.CreateBot(VoiceMessageJson);
        using (bot)
        {
            var message = await bot.SendVoiceAsync(123, new InputFile { FileId = "voice-file-id" }, default);

            var request = handler.LastRequest!;
            Assert.AreEqual(new Uri($"https://api.telegram.org/bot{TestBot.Token}/sendVoice"), request.RequestUri);
            var body = Encoding.UTF8.GetString(handler.LastRequestBody!);
            Assert.AreEqual("""{"chat_id":123,"voice":"voice-file-id"}""", body);

            var voice = message.Voice;
            Assert.IsNotNull(voice);
            Assert.AreEqual("voice-file-id", voice.FileId);
            Assert.AreEqual("voice-unique-id", voice.FileUniqueId);
            Assert.AreEqual(42, voice.Duration);
            Assert.AreEqual("audio/ogg", voice.MimeType);
            Assert.AreEqual(123456L, voice.FileSize);
        }
    }

    [TestMethod]
    public async Task SendVoice_WithExternalUrl_SendsJson()
    {
        var (bot, handler) = TestBot.CreateBot(VoiceMessageJson);
        using (bot)
        {
            await bot.SendVoiceAsync(123, new InputFile { ExternalUrl = "https://example.com/voice.ogg" }, default);

            var body = Encoding.UTF8.GetString(handler.LastRequestBody!);
            Assert.AreEqual("""{"chat_id":123,"voice":"https://example.com/voice.ogg"}""", body);
        }
    }

    [TestMethod]
    public async Task SendVoice_WithAllParameters_SendsJson()
    {
        var (bot, handler) = TestBot.CreateBot(VoiceMessageJson);
        using (bot)
        {
            var request = new Api.SendVoice(123, new InputFile { FileId = "voice-file-id" })
            {
                Duration = 42,
                Caption = "hello",
                ParseMode = ParseMode.Markdown,
                DisableNotification = true,
            };
            await bot.ExecuteAsync(request, default);

            var body = Encoding.UTF8.GetString(handler.LastRequestBody!);
            Assert.AreEqual("""{"chat_id":123,"voice":"voice-file-id","duration":42,"caption":"hello","parse_mode":"markdown","disable_notification":true}""", body);
        }
    }

    [TestMethod]
    public async Task SendVoice_WithContent_UploadsMultipartForm()
    {
        var (bot, handler) = TestBot.CreateBot(VoiceMessageJson);
        using (bot)
        {
            var content = new MemoryStream(Encoding.UTF8.GetBytes("audio-bytes"));
            var message = await bot.SendVoiceAsync(123, new InputFile { FileName = "voice.ogg", Content = content }, 42, "hello", default);

            var request = handler.LastRequest!;
            Assert.AreEqual(new Uri($"https://api.telegram.org/bot{TestBot.Token}/sendVoice"), request.RequestUri);
            Assert.IsTrue(handler.LastRequestContentType!.StartsWith("multipart/form-data", StringComparison.Ordinal));
            var boundary = handler.LastRequestContentType.Split("boundary=\"")[1].Split('"')[0];

            var body = Encoding.UTF8.GetString(handler.LastRequestBody!);
            var fields = TestBot.ParseMultipart(body, boundary);

            Assert.AreEqual("123", fields["chat_id"].Body);
            Assert.AreEqual("42", fields["duration"].Body);
            Assert.AreEqual("hello", fields["caption"].Body);

            var voice = fields["voice"];
            Assert.IsTrue(voice.Headers.Contains("filename=voice.ogg", StringComparison.Ordinal));
            Assert.IsTrue(voice.Headers.Contains("application/octet-stream", StringComparison.Ordinal));
            Assert.AreEqual("audio-bytes", voice.Body);

            Assert.IsNotNull(message.Voice);
        }
    }

    [TestMethod]
    public async Task GetFile_SendsFileId()
    {
        var (bot, handler) = TestBot.CreateBot(FileJson);
        using (bot)
        {
            var file = await bot.GetFileAsync("voice-file-id", default);

            var request = handler.LastRequest!;
            Assert.AreEqual(new Uri($"https://api.telegram.org/bot{TestBot.Token}/getFile"), request.RequestUri);
            var body = Encoding.UTF8.GetString(handler.LastRequestBody!);
            Assert.AreEqual("""{"file_id":"voice-file-id"}""", body);

            Assert.AreEqual("voice-file-id", file.FileId);
            Assert.AreEqual("voice-unique-id", file.FileUniqueId);
            Assert.AreEqual(12345678901L, file.FileSize);
            Assert.AreEqual("audio/abc.ogg", file.FilePath);
        }
    }

    [TestMethod]
    public async Task DownloadFileAsync_DownloadsFromRelativeUrl()
    {
        var handler = new TestBot.StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("audio-bytes"))
        });
        using var bot = new TelegramBot(TestBot.Token, TestBot.CreateHttpClient(handler), NullLogger<TelegramBot>.Instance);

        var file = new File { FileId = "voice-file-id", FileUniqueId = "voice-unique-id", FilePath = "audio/abc.ogg" };
        var stream = new MemoryStream();
        await bot.DownloadAsync(file, stream, default);
        stream.Seek(0, SeekOrigin.Begin);

        var request = handler.LastRequest!;
        Assert.AreEqual(HttpMethod.Get, request.Method);
        Assert.AreEqual(new Uri($"https://api.telegram.org/file/bot{TestBot.Token}/audio/abc.ogg"), request.RequestUri);
        using var reader = new StreamReader(stream);
        Assert.AreEqual("audio-bytes", reader.ReadToEnd());
    }

    [TestMethod]
    public void DownloadFileAsync_WithoutPath_Throws()
    {
        var handler = new TestBot.StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var bot = new TelegramBot(TestBot.Token, TestBot.CreateHttpClient(handler), NullLogger<TelegramBot>.Instance);

        var file = new File { FileId = "voice-file-id", FileUniqueId = "voice-unique-id" };

        Assert.ThrowsException<ArgumentException>(() => bot.DownloadAsync(file, Stream.Null, default).GetAwaiter().GetResult());
    }

    private const string VoiceMessageJson = """
        {
            "message_id": 1,
            "date": 1700000000,
            "chat": { "id": 123, "type": "private" },
            "voice": {
                "file_id": "voice-file-id",
                "file_unique_id": "voice-unique-id",
                "duration": 42,
                "mime_type": "audio/ogg",
                "file_size": 123456
            }
        }
        """;

    private const string FileJson = """
        {
            "file_id": "voice-file-id",
            "file_unique_id": "voice-unique-id",
            "file_size": 12345678901,
            "file_path": "audio/abc.ogg"
        }
        """;
}
