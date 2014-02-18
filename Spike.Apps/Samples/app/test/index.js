var Services = (function () {
    function Services() {
        this._services = getServices();
        this._timerId = null;
    }

    function getServices() {
        var obj1 = { serviceName: "foo", listeners: 5, protocols: [{ protocolName: "protocol1.1" }, { protocolName: "protocol1.2" }], cost: 5.3 };
        var obj2 = { serviceName: "bar", listeners: 8, protocols: [{ protocolName: "protocol2.1" }, { protocolName: "protocol2.2" }], cost: 4.1 };
        var obj3 = { serviceName: "goo", listeners: 2, protocols: [{ protocolName: "protocol3.1" }, { protocolName: "protocol3.2" }], cost: 8.5 };
        var index = Math.floor((Math.random() * 3) + 1);
        var services = [];
        if (index == 1)
            services = [obj1, obj2];
        if (index == 2)
            services = [obj3];
        if (index == 3)
            services = [obj3, obj2];
        return services;
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
        this._services.push({ serviceName: "goo", listeners: 2, protocols: [{ protocolName: "protocol3.1" }, { protocolName: "protocol3.2" }], cost: 8.5 });
        console.log(this._services);
    };
    return Services;
})();