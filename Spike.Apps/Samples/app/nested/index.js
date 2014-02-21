var Nested = (function () {
    function Nested() {
        this._people = [
            { name: 'Alan', surname: 'Turing' },
            { name: 'Dennis', surname: 'Ritchie' },
            { name: 'Bill', surname: 'Gates' }
        ];
        this._leader = {
            name: 'Roman',
            surname: 'Atachiants',
            role: {
                name: 'CEO',
                activity: 'Making coffee'
            }
        };
        this._team = {
            leader: this._leader,
            people: this._people
        };
    }
    Object.defineProperty(Nested.prototype, "team", {
        get: function () {
            return this._team;
        },
        set: function (v) {
            this._team = v;
        },
        enumerable: true,
        configurable: true
    });

    /**
    * Adds a member to the team list.
    */
    Nested.prototype.addMember = function (name, surname) {
        this._people.push({ name: name, surname: surname });
    };
    return Nested;
})();
