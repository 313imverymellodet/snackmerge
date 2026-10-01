// SNACK MERGE page side: daily + all-time leaderboard overlay and score submission.
// Unity calls window.snack.*; results go back via SendMessage("Game", "OnRank", json).
(function () {
  var qs = new URLSearchParams(location.search);
  var API = qs.get("api") || "https://orbyt-api-production-29f6.up.railway.app";
  var WS_LIVE = API.replace(/^http/, "ws") + "/live";
  var tokens = {}, meName = store("sm_name"), boardMode = null, live = null;

  function store(k, v) { try { if (v === undefined) return localStorage.getItem(k); localStorage.setItem(k, v); } catch (e) { return null; } }
  function playerId() {
    var id = store("sm_player");
    if (!id) { id = crypto.randomUUID ? crypto.randomUUID() : (Date.now().toString(16) + Math.random().toString(16).slice(2)); store("sm_player", id); }
    return id;
  }
  function toUnity(method, payload) { try { window.unityInstance && window.unityInstance.SendMessage("Game", method, JSON.stringify(payload)); } catch (e) {} }
  function post(path, body) { return fetch(API + path, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body || {}) }).then(function (r) { return r.json(); }); }
  function esc(s) { return String(s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); }
  function track(n, v) { window.SD && window.SD.track && window.SD.track(n, v || 0); }
  var SNACKS = ["CHERRY", "STRAWBERRY", "LEMON", "APPLE", "ORANGE", "DONUT", "COCONUT", "PIZZA", "CAKE", "PUMPKIN", "WATERMELON"];

  var css = document.createElement("style");
  css.textContent = [
    ".smov{position:fixed;inset:0;z-index:20;display:none;align-items:center;justify-content:center;background:rgba(90,58,42,.45);backdrop-filter:blur(3px);font-family:system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;color:#5a3a2a}",
    ".smov.on{display:flex}",
    ".smov .card{width:min(430px,92vw);max-height:88vh;display:flex;flex-direction:column;background:#fff7ec;border:4px solid #c98b5b;border-radius:26px;box-shadow:0 20px 60px rgba(90,58,42,.4);overflow:hidden;animation:smin .25s ease}",
    "@keyframes smin{from{transform:scale(.9);opacity:0}}",
    ".smov .hd{padding:18px 18px 10px;display:flex;align-items:center;justify-content:space-between}",
    ".smov h2{margin:0;font-size:28px;font-weight:900;font-style:italic;letter-spacing:1px;color:#ff5e7e;text-shadow:0 3px 0 #ffd6c2}",
    ".smov .x{background:#ffe3cc;border:0;color:#5a3a2a;width:40px;height:40px;border-radius:12px;font-size:18px;cursor:pointer}",
    ".smov .tabs{display:flex;gap:8px;padding:0 18px 10px}",
    ".smov .tab{flex:1;padding:12px;border-radius:14px;border:0;background:#ffe3cc;color:#5a3a2a;font-weight:900;font-style:italic;letter-spacing:1px;cursor:pointer}",
    ".smov .tab.on{background:#ff5e7e;color:#fff}",
    ".smov .live{padding:0 18px 8px;font-size:12px;opacity:.75;display:flex;align-items:center;gap:6px}",
    ".smov .dot{width:8px;height:8px;border-radius:50%;background:#3fc79a;box-shadow:0 0 8px #3fc79a;animation:smp 1.4s infinite}",
    "@keyframes smp{50%{opacity:.35}}",
    ".smov ol{list-style:none;margin:0;padding:0 10px 14px;overflow:auto}",
    ".smov li{display:flex;align-items:center;gap:10px;padding:10px;border-radius:12px;font-weight:700}",
    ".smov li:nth-child(odd){background:rgba(201,139,91,.1)}",
    ".smov li.me{background:rgba(255,94,126,.18);outline:2px solid rgba(255,94,126,.5)}",
    ".smov li.flash{animation:smf 1.2s ease}@keyframes smf{0%{background:rgba(255,184,51,.6)}}",
    ".smov .rk{width:30px;text-align:center;font-weight:900;font-style:italic;opacity:.8}",
    ".smov li:nth-child(1) .rk{color:#e09a00;opacity:1}.smov li:nth-child(2) .rk{color:#9aa4b5;opacity:1}.smov li:nth-child(3) .rk{color:#c97b3c;opacity:1}",
    ".smov .nm{flex:1;min-width:0}.smov .nm b{display:block;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.smov .nm small{opacity:.6;font-size:11px;letter-spacing:1px}",
    ".smov .tm{font-weight:900;font-size:19px;font-variant-numeric:tabular-nums}",
    ".smov .empty{padding:30px;text-align:center;opacity:.65}",
    ".smov .body{padding:4px 22px 22px;text-align:center}",
    ".smov .sub{opacity:.75;font-size:14px;margin:0 0 16px}",
    ".smov .btn{display:block;width:100%;padding:16px;margin:8px 0;border-radius:16px;border:0;font-weight:900;font-style:italic;font-size:18px;letter-spacing:1px;cursor:pointer}",
    ".smov .p{background:#ff5e7e;color:#fff}.smov .s{background:#ffe3cc;color:#5a3a2a}",
    ".smov input{width:100%;box-sizing:border-box;padding:14px;margin:8px 0 0;border-radius:14px;border:3px solid #ffb833;background:#fff;color:#5a3a2a;font-size:22px;font-weight:900;text-align:center;letter-spacing:4px;text-transform:uppercase;outline:none}",
    ".smov .err{color:#e0405a;min-height:18px;font-size:13px;margin-top:6px}"
  ].join("\n");
  document.head.appendChild(css);

  function overlay(id) {
    var o = document.createElement("div"); o.id = id; o.className = "smov";
    o.innerHTML = '<div class="card"></div>';
    document.body.appendChild(o);
    ["keydown", "keyup", "keypress"].forEach(function (t) { o.addEventListener(t, function (e) { e.stopPropagation(); }, true); });
    return o;
  }

  // ---------------------------------------------------------------- leaderboard
  var lb = overlay("smlb");
  lb.querySelector(".card").innerHTML = '<div class="hd"><h2>LEADERBOARD</h2><button class="x" aria-label="Close">&#10005;</button></div>' +
    '<div class="tabs"><button class="tab" data-m="daily">TODAY\'S JAR</button><button class="tab" data-m="classic">ALL-TIME</button></div>' +
    '<div class="live"><span class="dot"></span><span class="lbl">HIGH SCORES · LIVE</span></div><ol></ol>';
  var lbList = lb.querySelector("ol");
  lb.querySelector(".x").onclick = function () { lb.classList.remove("on"); boardMode = null; };
  lb.addEventListener("click", function (e) { if (e.target === lb) { lb.classList.remove("on"); boardMode = null; } });
  lb.querySelectorAll(".tab").forEach(function (b) { b.onclick = function () { showBoard(b.getAttribute("data-m")); }; });

  function renderBoard(rows, flash) {
    if (!rows.length) { lbList.innerHTML = '<div class="empty">No scores yet. Be the first!</div>'; return; }
    lbList.innerHTML = rows.map(function (r, i) {
      var me = meName && r.name === meName.toUpperCase();
      return '<li class="' + (me ? "me " : "") + (flash && r.name === flash ? "flash" : "") + '"><span class="rk">' + (i + 1) + '</span>' +
        '<span class="nm"><b>' + esc(r.name) + (me ? " (YOU)" : "") + '</b><small>MADE A ' + esc(SNACKS[r.tier] || "SNACK") + '</small></span>' +
        '<span class="tm">' + Number(r.score).toLocaleString() + "</span></li>";
    }).join("");
  }
  function loadBoard(mode, flash) {
    return fetch(API + "/api/snack/board?mode=" + mode + "&limit=30").then(function (r) { return r.json(); })
      .then(function (d) { if (boardMode === mode) renderBoard(d.top || [], flash); })
      .catch(function () { lbList.innerHTML = '<div class="empty">Leaderboard offline. Try again soon.</div>'; });
  }
  function showBoard(mode) {
    boardMode = mode === "classic" ? "classic" : "daily";
    lb.querySelectorAll(".tab").forEach(function (b) { b.classList.toggle("on", b.getAttribute("data-m") === boardMode); });
    lb.querySelector(".lbl").textContent = boardMode === "daily" ? "TODAY'S DAILY JAR · RESETS AT MIDNIGHT UTC" : "BEST CLASSIC SCORES · LIVE";
    lb.classList.add("on");
    lbList.innerHTML = '<div class="empty">Loading...</div>';
    connectLive();
    loadBoard(boardMode);
    track("lb_open");
  }
  function connectLive() {
    if (live && live.readyState <= 1) return;
    try { live = new WebSocket(WS_LIVE); } catch (e) { return; }
    live.onmessage = function (ev) {
      var m; try { m = JSON.parse(ev.data); } catch (e) { return; }
      if (m.type === "snack" && boardMode === m.mode) loadBoard(m.mode, m.name);
    };
    live.onclose = function () { live = null; if (boardMode) setTimeout(connectLive, 3000); };
  }

  // ---------------------------------------------------------------- name prompt
  var nm = overlay("smname");
  function askName(cb, err, onSkip) {
    nm.querySelector(".card").innerHTML = '<div class="body" style="padding-top:22px"><h2>YOUR NAME</h2><p class="sub" style="margin-top:8px">Shown on the leaderboard</p>' +
      '<input maxlength="12" autocomplete="off" autocapitalize="characters" spellcheck="false" placeholder="NAME">' +
      '<div class="err">' + (err || "") + '</div><button class="btn p" data-a="ok">SAVE MY SCORE</button><button class="btn s" data-a="skip">SKIP</button></div>';
    var input = nm.querySelector("input");
    input.value = meName || "";
    nm.classList.add("on");
    setTimeout(function () { input.focus(); }, 50);
    nm.querySelector('[data-a="ok"]').onclick = function () {
      var v = input.value.toUpperCase().replace(/[^A-Z0-9 _.-]/g, "").trim();
      if (v.length < 2) { nm.querySelector(".err").textContent = "At least 2 letters or numbers."; return; }
      meName = v; store("sm_name", v);
      nm.classList.remove("on");
      cb(v);
    };
    nm.querySelector('[data-a="skip"]').onclick = function () { nm.classList.remove("on"); if (onSkip) onSkip(); };
    input.onkeydown = function (e) { if (e.key === "Enter") nm.querySelector('[data-a="ok"]').click(); };
  }

  // ---------------------------------------------------------------- API for Unity
  window.snack = {
    ready: function () {},
    start: function (mode) {
      tokens[mode] = null;
      post("/api/snack/run", { mode: mode }).then(function (d) { tokens[mode] = d.token; }).catch(function () {});
    },
    submit: function (mode, score, drops, maxTier) {
      var t = tokens[mode]; tokens[mode] = null;
      if (!t || score <= 0) { toUnity("OnRank", { mode: mode, rank: 0, error: t ? "zero" : "offline" }); return; }
      var send = function (name) {
        post("/api/snack/finish", { token: t, player: playerId(), name: name, score: score, drops: drops, tier: maxTier })
          .then(function (r) {
            if (r.error === "bad name") { meName = null; store("sm_name", ""); askName(send, "That name isn't allowed. Try another."); return; }
            if (r.error) { toUnity("OnRank", { mode: mode, rank: 0, error: r.error }); return; }
            toUnity("OnRank", { mode: mode, rank: r.rank, total: r.total, best: r.best });
          }).catch(function () { toUnity("OnRank", { mode: mode, rank: 0, error: "offline" }); });
      };
      if (meName) send(meName); else askName(send, null, function () { toUnity("OnRank", { mode: mode, rank: 0, error: "skipped" }); });
    },
    board: showBoard
  };
})();
