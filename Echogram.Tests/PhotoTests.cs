namespace Echogram.Tests;

using System.Text;
using Echo.Telegram;

[TestClass]
public sealed class PhotoTests
{
    [TestMethod]
    public async Task SendPhoto_WithFileId_SendsJson()
    {
        var (bot, handler) = TestBot.CreateBot(PhotoMessageJson);
        using (bot)
        {
            var message = await bot.SendPhotoAsync(123, new InputFile { FileId = "photo-file-id" }, default);

            var request = handler.LastRequest!;
            Assert.AreEqual(new Uri($"https://api.telegram.org/bot{TestBot.Token}/sendPhoto"), request.RequestUri);
            var body = Encoding.UTF8.GetString(handler.LastRequestBody!);
            Assert.AreEqual("""{"chat_id":123,"photo":"photo-file-id"}""", body);

            var photo = message.Photo;
            Assert.AreEqual(2, photo.Length);
            Assert.AreEqual("photo-file-id", photo[0].FileId);
            Assert.AreEqual("photo-unique-id", photo[0].FileUniqueId);
            Assert.AreEqual(100, photo[0].Width);
            Assert.AreEqual(200, photo[0].Height);
            Assert.AreEqual(12345L, photo[0].FileSize);
            Assert.AreEqual("photo-file-id-large", photo[1].FileId);
            Assert.AreEqual(800, photo[1].Width);
            Assert.AreEqual(1600, photo[1].Height);
            Assert.AreEqual(123456L, photo[1].FileSize);
        }
    }

    [TestMethod]
    public async Task SendPhoto_WithAllParameters_SendsJson()
    {
        var (bot, handler) = TestBot.CreateBot(PhotoMessageJson);
        using (bot)
        {
            var request = new Api.SendPhoto(123, new InputFile { FileId = "photo-file-id" })
            {
                Caption = "hello",
                ParseMode = ParseMode.Markdown,
                ShowCaptionAboveMedia = true,
                HasSpoiler = true,
                DisableNotification = true,
            };
            await bot.ExecuteAsync(request, default);

            var body = Encoding.UTF8.GetString(handler.LastRequestBody!);
            Assert.AreEqual("""{"chat_id":123,"photo":"photo-file-id","caption":"hello","parse_mode":"markdown","show_caption_above_media":true,"has_spoiler":true,"disable_notification":true}""", body);
        }
    }

    [TestMethod]
    public async Task SendPhoto_WithContent_UploadsMultipartForm()
    {
        var (bot, handler) = TestBot.CreateBot(PhotoMessageJson);
        using (bot)
        {
            var content = new MemoryStream(Encoding.UTF8.GetBytes("photo-bytes"));
            var message = await bot.SendPhotoAsync(123, new InputFile { FileName = "photo.jpg", Content = content }, "hello", default);

            var request = handler.LastRequest!;
            Assert.AreEqual(new Uri($"https://api.telegram.org/bot{TestBot.Token}/sendPhoto"), request.RequestUri);
            Assert.IsTrue(handler.LastRequestContentType!.StartsWith("multipart/form-data", StringComparison.Ordinal));
            var boundary = handler.LastRequestContentType.Split("boundary=\"")[1].Split('"')[0];

            var body = Encoding.UTF8.GetString(handler.LastRequestBody!);
            var fields = TestBot.ParseMultipart(body, boundary);

            Assert.AreEqual("123", fields["chat_id"].Body);
            Assert.AreEqual("hello", fields["caption"].Body);

            var photo = fields["photo"];
            Assert.IsTrue(photo.Headers.Contains("filename=photo.jpg", StringComparison.Ordinal));
            Assert.IsTrue(photo.Headers.Contains("application/octet-stream", StringComparison.Ordinal));
            Assert.AreEqual("photo-bytes", photo.Body);

            Assert.AreEqual(2, message.Photo.Length);
        }
    }

    private const string PhotoMessageJson = """
        {
            "message_id": 1,
            "date": 1700000000,
            "chat": { "id": 123, "type": "private" },
            "photo": [
                {
                    "file_id": "photo-file-id",
                    "file_unique_id": "photo-unique-id",
                    "width": 100,
                    "height": 200,
                    "file_size": 12345
                },
                {
                    "file_id": "photo-file-id-large",
                    "file_unique_id": "photo-unique-id-large",
                    "width": 800,
                    "height": 1600,
                    "file_size": 123456
                }
            ]
        }
        """;
}
