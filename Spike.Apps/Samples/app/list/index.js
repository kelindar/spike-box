var List = (function () {
    function List() {
        this._items = [];
    }
    Object.defineProperty(List.prototype, "items", {
        get: function () {
            return this._items;
        },
        enumerable: true,
        configurable: true
    });

    /**
    * Make a new random number between 0 and 100
    */
    List.prototype._rand = function () {
        return {
            value: Math.round(Math.random() * 100, 0)
        };
    };

    /**
    * The push() method adds new items to the end of an array, and returns the new length.
    */
    List.prototype.push = function () {
        this._items.push(this._rand());
    };

    /**
    * The pop() method removes the last element of an array, and returns that element.
    */
    List.prototype.pop = function () {
        return this._items.pop();
    };

    /**
    * The shift() method removes the first item of an array, and returns that item.
    */
    List.prototype.shift = function () {
        return this._items.shift();
    };

    /**
    * The splice() method adds/removes items to/from an array, and returns the removed item(s).
    */
    List.prototype.splice = function () {
        this._items.splice(1, 2, this._rand());
    };

    /**
    * The swap() method swaps two elements.
    */
    List.prototype.swap = function () {
        this._items.swap(0, this._items.length - 1);
    };

    /**
    * The clear() method clears the array.
    */
    List.prototype.clear = function () {
        this._items.clear();
    };

    /**
    * The swap() method swaps two elements.
    */
    List.prototype.removeAll = function () {
        this._items.removeAll(function (i) {
            return i.Number > 50;
        });
    };
    return List;
})();
