var Services = (function () {
    function Services() {
        this._services = [
			{ serviceName: "foo", listeners: 5, protocols: [{ protocolName: "protocol1.1" }, { protocolName: "protocol1.2" }], cost: 5.3 }
			];
        this._timerId = null;
    }

    Services.prototype.updateServices = function() {
        var obj1 = { serviceName: "foo", listeners: 5, protocols: [{ protocolName: "protocol1.1" }, { protocolName: "protocol1.2" }], cost: 5.3 };
		
        var obj2 = { serviceName: "bar", listeners: 8, protocols: [{ protocolName: "protocol2.1" }, { protocolName: "protocol2.2" }], cost: 4.1 };
		
        var obj3 = { serviceName: "goo", listeners: 2, protocols: [{ protocolName: "protocol3.1" }, { protocolName: "protocol3.2" }], cost: 8.5 };
		
		this._services.push(obj1);
		this._services.push(obj2);
		
		/*var socket = new JsonOverTCP();
		socket.onConnect(function(m){
			// ...
		});

		socket.onMessage(function(m){
			// ...
			m.reply({ text: 'test'});
		});*/
    }

    Object.defineProperty(Services.prototype, "services", {
        get: function () {
            return this._services;
        },
        enumerable: true,
        configurable: true
    });
	
    Services.prototype.toggleInterval = function () {
        console.log("function called");

        this.updateServices();
        console.log(this._services);
    };
    return Services;
})();