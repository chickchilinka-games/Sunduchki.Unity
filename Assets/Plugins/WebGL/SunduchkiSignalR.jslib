mergeInto(LibraryManager.library, {
    $sunduchkiSignalR: {
        init: function () {
            if (typeof Module.SunduchkiSignalR !== 'undefined') {
                return;
            }

            Module.SunduchkiSignalR = {
                nextId: 1,
                connections: {}
            };
        },

        ensureClient: function () {
            if (typeof signalR === 'undefined' || !signalR.HubConnectionBuilder) {
                throw new Error('signalR client is not available. Include @microsoft/signalr script.');
            }
        },

        getHandle: function (id) {
            if (typeof Module.SunduchkiSignalR === 'undefined') {
                return null;
            }

            return Module.SunduchkiSignalR.connections[id] || null;
        },

        removeHandle: function (id) {
            if (typeof Module.SunduchkiSignalR === 'undefined') {
                return;
            }

            delete Module.SunduchkiSignalR.connections[id];
        },

        sendMessage: function (goName, payload) {
            var instance = typeof unityInstance !== 'undefined' ? unityInstance : (typeof Module !== 'undefined' ? Module : null);
            if (!instance || typeof instance.SendMessage !== 'function') {
                console.warn('[SignalR.jslib] Unity instance not found.');
                return;
            }

            try {
                instance.SendMessage(goName, 'OnSignalRMessage', payload || '');
            } catch (err) {
                console.warn('[SignalR.jslib] SendMessage failed:', err);
            }
        },

        sendEnvelope: function (handle, envelope) {
            envelope.connectionId = handle.id;
            sunduchkiSignalR.sendMessage(handle.gameObjectName, JSON.stringify(envelope));
        }
    },

    Sunduchki_SignalR_CreateConnection: function (urlPtr, tokenPtr, hostPtr) {
        try {
            sunduchkiSignalR.init();
            sunduchkiSignalR.ensureClient();
        } catch (err) {
            console.error('[SignalR.jslib] Initialization failed:', err);
            return -1;
        }

        var url = UTF8ToString(urlPtr || 0);
        var token = tokenPtr ? UTF8ToString(tokenPtr) : null;
        var hostName = UTF8ToString(hostPtr || 0);

        try {
            var builder = new signalR.HubConnectionBuilder().withUrl(url, token ? {
                accessTokenFactory: function () {
                    return token;
                }
            } : undefined).withAutomaticReconnect();

            var connection = builder.build();
            var id = Module.SunduchkiSignalR.nextId++;

            var handle = {
                id: id,
                connection: connection,
                gameObjectName: hostName,
                handlers: {}
            };

            Module.SunduchkiSignalR.connections[id] = handle;

            connection.onclose(function (error) {
                sunduchkiSignalR.sendEnvelope(handle, {
                    type: 'closed',
                    error: error ? error.toString() : null
                });
                sunduchkiSignalR.removeHandle(id);
            });

            return id;
        } catch (err) {
            console.error('[SignalR.jslib] Failed to create connection:', err);
            return -1;
        }
    },

    Sunduchki_SignalR_Start: function (connectionId) {
        var handle = sunduchkiSignalR.getHandle(connectionId);
        if (!handle) {
            return;
        }

        handle.connection.start()
            .then(function () {
                sunduchkiSignalR.sendEnvelope(handle, { type: 'started' });
            })
            .catch(function (err) {
                sunduchkiSignalR.sendEnvelope(handle, {
                    type: 'startFailed',
                    error: err ? err.toString() : 'Unknown error'
                });
            });
    },

    Sunduchki_SignalR_Stop: function (connectionId) {
        var handle = sunduchkiSignalR.getHandle(connectionId);
        if (!handle) {
            return;
        }

        handle.connection.stop()
            .then(function () {
                sunduchkiSignalR.sendEnvelope(handle, { type: 'stopped' });
            })
            .catch(function (err) {
                sunduchkiSignalR.sendEnvelope(handle, {
                    type: 'stopped',
                    error: err ? err.toString() : null
                });
            })
            .finally(function () {
                sunduchkiSignalR.removeHandle(connectionId);
            });
    },

    Sunduchki_SignalR_RegisterHandler: function (connectionId, handlerPtr) {
        var handle = sunduchkiSignalR.getHandle(connectionId);
        if (!handle) {
            return;
        }

        var handlerName = UTF8ToString(handlerPtr || 0);
        if (!handlerName) {
            return;
        }

        if (handle.handlers[handlerName]) {
            return;
        }

        var callback = function () {
            var argsJson = JSON.stringify(Array.prototype.slice.call(arguments));
            sunduchkiSignalR.sendEnvelope(handle, {
                type: 'handler',
                handler: handlerName,
                args: argsJson
            });
        };

        handle.handlers[handlerName] = callback;
        handle.connection.on(handlerName, callback);
    },

    Sunduchki_SignalR_UnregisterHandler: function (connectionId, handlerPtr) {
        var handle = sunduchkiSignalR.getHandle(connectionId);
        if (!handle) {
            return;
        }

        var handlerName = UTF8ToString(handlerPtr || 0);
        if (!handlerName) {
            return;
        }

        var callback = handle.handlers[handlerName];
        if (!callback) {
            return;
        }

        handle.connection.off(handlerName, callback);
        delete handle.handlers[handlerName];
    },

    Sunduchki_SignalR_Invoke: function (connectionId, requestIdPtr, methodPtr, argsJsonPtr) {
        var handle = sunduchkiSignalR.getHandle(connectionId);
        if (!handle) {
            return;
        }

        var methodName = UTF8ToString(methodPtr || 0);
        var requestId = UTF8ToString(requestIdPtr || 0);
        var argsJson = argsJsonPtr ? UTF8ToString(argsJsonPtr) : null;

        var argsArray = [];
        if (argsJson) {
            try {
                var parsed = JSON.parse(argsJson);
                if (Array.isArray(parsed)) {
                    argsArray = parsed;
                } else if (parsed !== undefined && parsed !== null) {
                    argsArray = [parsed];
                }
            } catch (err) {
                console.warn('[SignalR.jslib] Failed to parse invoke args:', err);
            }
        }

        handle.connection.invoke.apply(handle.connection, [methodName].concat(argsArray))
            .then(function (result) {
                sunduchkiSignalR.sendEnvelope(handle, {
                    type: 'invokeResult',
                    requestId: requestId,
                    success: true,
                    result: result !== undefined ? JSON.stringify(result) : null
                });
            })
            .catch(function (err) {
                sunduchkiSignalR.sendEnvelope(handle, {
                    type: 'invokeResult',
                    requestId: requestId,
                    success: false,
                    error: err ? err.toString() : 'Invoke failed'
                });
            });
    }
});
