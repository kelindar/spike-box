var Compose = (function () {
    function Compose() {
        this._text = "I'm the main component!";
    }
    Object.defineProperty(Compose.prototype, "text", {
        get: function () {
            return this._text;
        },
        enumerable: true,
        configurable: true
    });
    return Compose;
})();
