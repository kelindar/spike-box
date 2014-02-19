class Login{

    private _info = { authenticated: false };
    public get info() { return this._info; }

    private _username = "";
    public get username() { return this._username; }
    public set username(v) { this._username = v; }

    private _password = "";
    public get password() { return this._password; }
    public set password(v)
    {
        this._password = v;
        this._login(this._username, this._password);
    }


    private _login(username, password) {
        console.log("username = " + username);
        console.log("password = " + password);

        this._info.authenticated = (password.length > 5) ? true : false;
            
    }




} 