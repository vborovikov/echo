namespace Echogram.Tests;

using Echo.Telegram;

[TestClass]
public sealed class ChatIdTests
{
    [DataTestMethod]
    [DataRow("@channelusername", "@channelusername")]
    [DataRow("channelusername", "@channelusername")]
    [DataRow("123", "123")]
    [DataRow(123, "123")]
    public void ConvertFrom_IdOrName_Converted(object identity, string str)
    {
        var chatId = ChatId.ConvertFrom(identity);

        Assert.AreNotEqual(default, chatId);
        Assert.AreEqual(str, chatId.ToString());
    }
}
