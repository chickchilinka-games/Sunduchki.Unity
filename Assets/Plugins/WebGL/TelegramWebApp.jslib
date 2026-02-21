mergeInto(LibraryManager.library, {
  TelegramWebAppGetInitData: function () {
    var data = "";
    try {
      if (window.Telegram && window.Telegram.WebApp && window.Telegram.WebApp.initData) {
        data = window.Telegram.WebApp.initData;
      }
    } catch (e) {
      console.error(e);
      data = "";
    }
    var length = lengthBytesUTF8(data) + 1;
    var buffer = _malloc(length);
    stringToUTF8(data, buffer, length);
    console.log(data);
    return buffer;
  }
});
