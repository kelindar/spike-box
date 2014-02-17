var ComposeOther = (function () {
    function ComposeOther() {
        this._text = "I'm a sub-component!";
    }
    Object.defineProperty(ComposeOther.prototype, "text", {
        get: function () {
            return this._text;
        },
        enumerable: true,
        configurable: true
    });
    return ComposeOther;
})();
