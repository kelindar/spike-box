Spike.Box
=========

Spike.Box is an experimental web server which allows developers to directly data-bind remote client's DOM to server-side exercuted javascript. This project is not currently production-ready and is an exploratory study of the future of the web development.

* Build Status:[![Build status](https://ci.appveyor.com/api/projects/status/ja6fpmelifl4jeos?svg=true)](https://ci.appveyor.com/project/Kelindar/spike-box)
* NuGet Package: [![Spike.Box](https://img.shields.io/nuget/dt/Spike.Box.svg)](https://www.nuget.org/packages/Spike.Box/)


It is internally composed of serveral features:
 * A modified [IronJS](https://github.com/fholm/IronJS) runtime that executes javascript and monitors for dynamic changes in the state of JavaScript objects. For example, any modification of an in-memory javascript object or an array will be automatically and in real-time pushed to the remote client.
 * [Spike Engine](http://www.spike-engine.com) is used to communicate in real-time with the browser using a thin websocket layer.
 * Google's [AngularJs](https://angularjs.org) with a interoperability layer is used to data bind in-browser memory state to the DOM.
 
 
