var Hello = (function () {
    function Hello() {
        this._name = "World";
    }
    Object.defineProperty(Hello.prototype, "name", {
        get: function () {
            return this._name;
        },
        enumerable: true,
        configurable: true
    });

    /**
    * This function sets a name property and echoes a value.
    */
    Hello.prototype.sayHello = function (name) {
        // Set the name to the new name
        this._name = name;

        // Return the greeting, will be available in result.sayHello variable
        return "Hello, " + name + "!";
    };
    return Hello;
})();
