mergeInto(LibraryManager.library, {
  AdeebFirebase_Request: function(targetPtr, idPtr, operationPtr, payloadPtr, urlPtr) {
    var target = UTF8ToString(targetPtr), id = UTF8ToString(idPtr);
    var operation = UTF8ToString(operationPtr), payload = UTF8ToString(payloadPtr), url = UTF8ToString(urlPtr);
    window.adeebPendingTargets = window.adeebPendingTargets || {};
    window.adeebPendingTargets[id] = target;
    function deliver(data) {
      if (window.adeebPendingTargets[id] !== target) return;
      delete window.adeebPendingTargets[id];
      SendMessage(target, "OnResponse", JSON.stringify(data));
    }
    if (!window.adeebFirebaseModule) {
      window.adeebFirebaseModule = new Promise(function(resolve, reject) {
        var script = document.createElement("script");
        script.type = "module"; script.src = url;
        script.onload = function() { if (window.AdeebFirebase) resolve(window.AdeebFirebase); else reject(new Error("Firebase module failed to initialize.")); };
        script.onerror = function() { script.remove(); reject(new Error("Could not load Firebase. Check your connection and retry.")); };
        document.head.appendChild(script);
      }).catch(function(error) { window.adeebFirebaseModule = null; throw error; });
    }
    window.adeebFirebaseModule.then(function(bridge) { return bridge.execute(operation, payload); })
      .then(function(data) { data.requestId = id; data.success = true; deliver(data); })
      .catch(function(error) { deliver({ requestId:id, success:false, error:error.message || "Firebase request failed." }); });
  },
  AdeebFirebase_CancelTarget: function(targetPtr) {
    var target = UTF8ToString(targetPtr), pending = window.adeebPendingTargets || {};
    Object.keys(pending).forEach(function(id) { if (pending[id] === target) delete pending[id]; });
  }
});
