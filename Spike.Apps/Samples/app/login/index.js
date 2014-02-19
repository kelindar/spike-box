var Login = (function () {
    function Login() {
        this._info = { authenticated: false };
        this._username = "";
        this._password = "";
    }
    Object.defineProperty(Login.prototype, "info", {
        get: function () {
            return this._info;
        },
        enumerable: true,
        configurable: true
    });

    Object.defineProperty(Login.prototype, "username", {
        get: function () {
            return this._username;
        },
        set: function (v) {
            this._username = v;
        },
        enumerable: true,
        configurable: true
    });

    Object.defineProperty(Login.prototype, "password", {
        get: function () {
            return this._password;
        },
        set: function (v) {
            this._password = v;
            this._login(this._username, this._password);
        },
        enumerable: true,
        configurable: true
    });

    Login.prototype._login = function (username, password) {
        console.log("username = " + username);
        console.log("password = " + password);

        this._info.authenticated = (password.length > 5) ? true : false;
    };
    return Login;
})();
