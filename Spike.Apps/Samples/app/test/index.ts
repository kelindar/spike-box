class Hello{

    private _name = "World";

    public get name() { return this._name; }

    /**
    * This function sets a name property and echoes a value.
    */
    public sayHello(name) {
        // Set the name to the new name
        this._name = name;

        // Return the greeting, will be available in result.sayHello variable
        return "Hello, " + name + "!";
    }


} 