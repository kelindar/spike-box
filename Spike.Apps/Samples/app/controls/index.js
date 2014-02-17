var Controls = (function () {
    function Controls() {
        this._text = "I'm the main component!";
    }
    Object.defineProperty(Controls.prototype, "text", {
        get: function () {
            return this._text;
        },
        enumerable: true,
        configurable: true
    });
    return Controls;
})();
