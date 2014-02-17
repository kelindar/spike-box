class Nested{

    private _people = [
        { name: 'Alan', surname: 'Turing' },
        { name: 'Dennis', surname: 'Ritchie' },
        { name: 'Bill', surname: 'Gates' }
    ];

	
	private _leader = {
        name: 'Roman',
        surname: 'Atachiants',
        role: {
            name: 'CEO',
            activity: 'Making coffee'
        }
    }

    private _team = {
        leader: this._leader,
        people: this._people
    }
	
    public get team() { return this._team; }
    public set team(v) { this._team = v; }




} 