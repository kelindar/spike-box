var Form = (function () {
    function Form() {
        this._name = "visitor";
        this._age = null;
    }
    Object.defineProperty(Form.prototype, "name", {
        get: function () {
            return this._name;
        },
        enumerable: true,
        configurable: true
    });
    Object.defineProperty(Form.prototype, "age", {
        get: function () {
            return this._age;
        },
        enumerable: true,
        configurable: true
    });

    /**
    * Submits the form data to the server.
    */
    Form.prototype.submitData = function (name, age) {
        console.log(name);

        // Parse the age
        var parsedAge = parseInt(age);

        // Have some fun
        var second;
        if (isNaN(parsedAge)) {
            second = "How old are you?";
        } else {
            if (parsedAge < 0)
                second = "You're not yet born!";
            else if (parsedAge < 2)
                second = "You're a baby!";
            else if (parsedAge < 14)
                second = "You're a kid!";
            else if (parsedAge < 18)
                second = "You're a teen!";
            else if (parsedAge < 60)
                second = "You're an adult!";
            else if (parsedAge < 120)
                second = "You're a senior!";
            else
                second = "You're probably dead!";
        }

        // Return
        return "Dear, " + name + ". " + second;
    };
    return Form;
})();
