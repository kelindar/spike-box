var Table = (function () {
    function Table() {
        this._id = 1;
        this._items = [
            { id: 1, name: "Bananas", quantity: 1, status: "shipped", type: "success" }
        ];
    }
    Object.defineProperty(Table.prototype, "items", {
        get: function () {
            return this._items;
        },
        set: function (v) {
            this._items = v;
        },
        enumerable: true,
        configurable: true
    });

    /**
    * Make a new random object.
    */
    Table.prototype._new = function () {
        this._id++;

        var rnd1 = Math.round(Math.random() * 4, 0);
        var rnd2 = Math.round(Math.random() * 5, 0);
        var rnd3 = Math.round(Math.random() * 9, 0);

        var name;
        switch (rnd1) {
            case 0:
                name = "Bananas";
                break;
            case 1:
                name = "Apples";
                break;
            case 2:
                name = "Oranges";
                break;
            case 3:
                name = "Berries";
                break;
            case 4:
                name = "Melons";
                break;
        }

        var status;
        var type;
        switch (rnd2) {
            case 0:
            case 1:
            case 2:
                status = "shipped";
                type = "warning";
                break;
            case 3:
                status = "delivered";
                type = "success";
                break;
            case 4:
                status = "returned";
                type = "success";
                break;
            case 5:
                status = "lost in transit";
                type = "danger";
                break;
        }

        return {
            id: this._id,
            name: name,
            quantity: rnd3 + 1,
            status: status,
            type: type
        };
    };

    /**
    * Adds a new random order to the list
    */
    Table.prototype.newOrder = function () {
        this._items.push(this._new());
    };

    /**
    * Marks all shipped orders as delivered.
    */
    Table.prototype.deliver = function () {
        this._items.forEach(function (item) {
            if (item.status == "shipped") {
                item.status = "delivered";
                item.type = "success";
            }
        });
    };

    /**
    * The pop() method removes the last element of an array, and returns that element.
    */
    Table.prototype.archive = function () {
        return this._items.removeAll(function (item) {
            return item.type == "success" || item.type == "danger";
        });
    };
    return Table;
})();
