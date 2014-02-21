class List{

    private _items = [];
    
    public get items() { return this._items; }

    /**
    * Make a new random number between 0 and 100
    */
    private _rand() {
        return {
            value: Math.round(Math.random() * 100, 0)
        };
    }

    /**
    * The push() method adds new items to the end of an array, and returns the new length.
    */
    public push() {
        this._items.push(this._rand());
    }

    /**
    * The pop() method removes the last element of an array, and returns that element.
    */
    public pop() {
        return this._items.pop();
    }

    /**
    * The shift() method removes the first item of an array, and returns that item.
    */
    public shift() {
        return this._items.shift();
    }

    /**
    * The splice() method adds/removes items to/from an array, and returns the removed item(s).
    */
    public splice() {
        this._items.splice(1, 2, this._rand());
    }

    /**
    * The swap() method swaps two elements.
    */
    public swap() {
        this._items.swap(0, this._items.length - 1);
    }

    /**
    * The clear() method clears the array.
    */
    public clear() {
        this._items.clear();
    }

    /**
    * The swap() method swaps two elements.
    */
    public removeAll() {
        this._items.removeAll(function (i) {
            return i.Number > 50;
        });
    }

} 