class Recurse{

    private _nodeA = {
        text: "node A",
        next: this._nodeB
    };

    private _nodeB = {
        text: "node B",
        next: this._nodeA
    };

    public get node()
    {
        if (this._nodeA.next == null)
            this._nodeA.next = this._nodeB;
        return this._nodeB;
    }



} 