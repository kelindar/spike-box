var Recurse = (function () {
    function Recurse() {
        this._nodeA = {
            text: "node A",
            next: this._nodeB
        };
        this._nodeB = {
            text: "node B",
            next: this._nodeA
        };
    }
    Object.defineProperty(Recurse.prototype, "node", {
        get: function () {
            if (this._nodeA.next == null)
                this._nodeA.next = this._nodeB;
            return this._nodeB;
        },
        enumerable: true,
        configurable: true
    });
    return Recurse;
})();
