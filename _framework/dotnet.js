//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

var e=!1;const t=async()=>WebAssembly.validate(new Uint8Array([0,97,115,109,1,0,0,0,1,4,1,96,0,0,3,2,1,0,10,8,1,6,0,6,64,25,11,11])),o=async()=>WebAssembly.validate(new Uint8Array([0,97,115,109,1,0,0,0,1,5,1,96,0,1,123,3,2,1,0,10,15,1,13,0,65,1,253,15,65,2,253,15,253,128,2,11])),n=async()=>WebAssembly.validate(new Uint8Array([0,97,115,109,1,0,0,0,1,5,1,96,0,1,123,3,2,1,0,10,10,1,8,0,65,0,253,15,253,98,11])),r=Symbol.for("wasm promise_control");function i(e,t){let o=null;const n=new Promise((function(n,r){o={isDone:!1,promise:null,resolve:t=>{o.isDone||(o.isDone=!0,n(t),e&&e())},reject:e=>{o.isDone||(o.isDone=!0,r(e),t&&t())}}}));o.promise=n;const i=n;return i[r]=o,{promise:i,promise_control:o}}function s(e){return e[r]}function a(e){e&&function(e){return void 0!==e[r]}(e)||Be(!1,"Promise is not controllable")}const l="__mono_message__",c=["debug","log","trace","warn","info","error"],d="MONO_WASM: ";let u,f,m,g,p,h;function w(e){g=e}function b(e){if(Pe.diagnosticTracing){const t="function"==typeof e?e():e;console.debug(d+t)}}function y(e,...t){console.info(d+e,...t)}function v(e,...t){console.info(e,...t)}function E(e,...t){console.warn(d+e,...t)}function _(e,...t){if(t&&t.length>0&&t[0]&&"object"==typeof t[0]){if(t[0].silent)return;if(t[0].toString)return void console.error(d+e,t[0].toString())}console.error(d+e,...t)}function x(e,t,o){return function(...n){try{let r=n[0];if(void 0===r)r="undefined";else if(null===r)r="null";else if("function"==typeof r)r=r.toString();else if("string"!=typeof r)try{r=JSON.stringify(r)}catch(e){r=r.toString()}t(o?JSON.stringify({method:e,payload:r,arguments:n.slice(1)}):[e+r,...n.slice(1)])}catch(e){m.error(`proxyConsole failed: ${e}`)}}}function j(e,t,o){f=t,g=e,m={...t};const n=`${o}/console`.replace("https://","wss://").replace("http://","ws://");u=new WebSocket(n),u.addEventListener("error",A),u.addEventListener("close",S),function(){for(const e of c)f[e]=x(`console.${e}`,T,!0)}()}function R(e){let t=30;const o=()=>{u?0==u.bufferedAmount||0==t?(e&&v(e),function(){for(const e of c)f[e]=x(`console.${e}`,m.log,!1)}(),u.removeEventListener("error",A),u.removeEventListener("close",S),u.close(1e3,e),u=void 0):(t--,globalThis.setTimeout(o,100)):e&&m&&m.log(e)};o()}function T(e){u&&u.readyState===WebSocket.OPEN?u.send(e):m.log(e)}function A(e){m.error(`[${g}] proxy console websocket error: ${e}`,e)}function S(e){m.debug(`[${g}] proxy console websocket closed: ${e}`,e)}function D(){Pe.preferredIcuAsset=O(Pe.config);let e="invariant"==Pe.config.globalizationMode;if(!e)if(Pe.preferredIcuAsset)Pe.diagnosticTracing&&b("ICU data archive(s) available, disabling invariant mode");else{if("custom"===Pe.config.globalizationMode||"all"===Pe.config.globalizationMode||"sharded"===Pe.config.globalizationMode){const e="invariant globalization mode is inactive and no ICU data archives are available";throw _(`ERROR: ${e}`),new Error(e)}Pe.diagnosticTracing&&b("ICU data archive(s) not available, using invariant globalization mode"),e=!0,Pe.preferredIcuAsset=null}const t="DOTNET_SYSTEM_GLOBALIZATION_INVARIANT",o=Pe.config.environmentVariables;if(void 0===o[t]&&e&&(o[t]="1"),void 0===o.TZ)try{const e=Intl.DateTimeFormat().resolvedOptions().timeZone||null;e&&(o.TZ=e)}catch(e){y("failed to detect timezone, will fallback to UTC")}}function O(e){var t;if((null===(t=e.resources)||void 0===t?void 0:t.icu)&&"invariant"!=e.globalizationMode){const t=e.applicationCulture||(ke?globalThis.navigator&&globalThis.navigator.languages&&globalThis.navigator.languages[0]:Intl.DateTimeFormat().resolvedOptions().locale),o=e.resources.icu;let n=null;if("custom"===e.globalizationMode){if(o.length>=1)return o[0].name}else t&&"all"!==e.globalizationMode?"sharded"===e.globalizationMode&&(n=function(e){const t=e.split("-")[0];return"en"===t||["fr","fr-FR","it","it-IT","de","de-DE","es","es-ES"].includes(e)?"icudt_EFIGS.dat":["zh","ko","ja"].includes(t)?"icudt_CJK.dat":"icudt_no_CJK.dat"}(t)):n="icudt.dat";if(n)for(let e=0;e<o.length;e++){const t=o[e];if(t.virtualPath===n)return t.name}}return e.globalizationMode="invariant",null}(new Date).valueOf();const C=class{constructor(e){this.url=e}toString(){return this.url}};async function k(e,t){try{const o="function"==typeof globalThis.fetch;if(Se){const n=e.startsWith("file://");if(!n&&o)return globalThis.fetch(e,t||{credentials:"same-origin"});p||(h=Ne.require("url"),p=Ne.require("fs")),n&&(e=h.fileURLToPath(e));const r=await p.promises.readFile(e);return{ok:!0,headers:{length:0,get:()=>null},url:e,arrayBuffer:()=>r,json:()=>JSON.parse(r),text:()=>{throw new Error("NotImplementedException")}}}if(o)return globalThis.fetch(e,t||{credentials:"same-origin"});if("function"==typeof read)return{ok:!0,url:e,headers:{length:0,get:()=>null},arrayBuffer:()=>new Uint8Array(read(e,"binary")),json:()=>JSON.parse(read(e,"utf8")),text:()=>read(e,"utf8")}}catch(t){return{ok:!1,url:e,status:500,headers:{length:0,get:()=>null},statusText:"ERR28: "+t,arrayBuffer:()=>{throw t},json:()=>{throw t},text:()=>{throw t}}}throw new Error("No fetch implementation available")}function I(e){return"string"!=typeof e&&Be(!1,"url must be a string"),!M(e)&&0!==e.indexOf("./")&&0!==e.indexOf("../")&&globalThis.URL&&globalThis.document&&globalThis.document.baseURI&&(e=new URL(e,globalThis.document.baseURI).toString()),e}const U=/^[a-zA-Z][a-zA-Z\d+\-.]*?:\/\//,P=/[a-zA-Z]:[\\/]/;function M(e){return Se||Ie?e.startsWith("/")||e.startsWith("\\")||-1!==e.indexOf("///")||P.test(e):U.test(e)}let L,N=0;const $=[],z=[],W=new Map,F={"js-module-threads":!0,"js-module-runtime":!0,"js-module-dotnet":!0,"js-module-native":!0,"js-module-diagnostics":!0},B={...F,"js-module-library-initializer":!0},V={...F,dotnetwasm:!0,heap:!0,manifest:!0},q={...B,manifest:!0},H={...B,dotnetwasm:!0},J={dotnetwasm:!0,symbols:!0},Z={...B,dotnetwasm:!0,symbols:!0},Q={symbols:!0};function G(e){return!("icu"==e.behavior&&e.name!=Pe.preferredIcuAsset)}function K(e,t,o){null!=t||(t=[]),Be(1==t.length,`Expect to have one ${o} asset in resources`);const n=t[0];return n.behavior=o,X(n),e.push(n),n}function X(e){V[e.behavior]&&W.set(e.behavior,e)}function Y(e){Be(V[e],`Unknown single asset behavior ${e}`);const t=W.get(e);if(t&&!t.resolvedUrl)if(t.resolvedUrl=Pe.locateFile(t.name),F[t.behavior]){const e=ge(t);e?("string"!=typeof e&&Be(!1,"loadBootResource response for 'dotnetjs' type should be a URL string"),t.resolvedUrl=e):t.resolvedUrl=ce(t.resolvedUrl,t.behavior)}else if("dotnetwasm"!==t.behavior)throw new Error(`Unknown single asset behavior ${e}`);return t}function ee(e){const t=Y(e);return Be(t,`Single asset for ${e} not found`),t}let te=!1;async function oe(){if(!te){te=!0,Pe.diagnosticTracing&&b("mono_download_assets");try{const e=[],t=[],o=(e,t)=>{!Z[e.behavior]&&G(e)&&Pe.expected_instantiated_assets_count++,!H[e.behavior]&&G(e)&&(Pe.expected_downloaded_assets_count++,t.push(se(e)))};for(const t of $)o(t,e);for(const e of z)o(e,t);Pe.allDownloadsQueued.promise_control.resolve(),Promise.all([...e,...t]).then((()=>{Pe.allDownloadsFinished.promise_control.resolve()})).catch((e=>{throw Pe.err("Error in mono_download_assets: "+e),Xe(1,e),e})),await Pe.runtimeModuleLoaded.promise;const n=async e=>{const t=await e;if(t.buffer){if(!Z[t.behavior]){t.buffer&&"object"==typeof t.buffer||Be(!1,"asset buffer must be array-like or buffer-like or promise of these"),"string"!=typeof t.resolvedUrl&&Be(!1,"resolvedUrl must be string");const e=t.resolvedUrl,o=await t.buffer,n=new Uint8Array(o);pe(t),await Ue.beforeOnRuntimeInitialized.promise,Ue.instantiate_asset(t,e,n)}}else J[t.behavior]?("symbols"===t.behavior&&(await Ue.instantiate_symbols_asset(t),pe(t)),J[t.behavior]&&++Pe.actual_downloaded_assets_count):(t.isOptional||Be(!1,"Expected asset to have the downloaded buffer"),!H[t.behavior]&&G(t)&&Pe.expected_downloaded_assets_count--,!Z[t.behavior]&&G(t)&&Pe.expected_instantiated_assets_count--)},r=[],i=[];for(const t of e)r.push(n(t));for(const e of t)i.push(n(e));Promise.all(r).then((()=>{Ce||Ue.coreAssetsInMemory.promise_control.resolve()})).catch((e=>{throw Pe.err("Error in mono_download_assets: "+e),Xe(1,e),e})),Promise.all(i).then((async()=>{Ce||(await Ue.coreAssetsInMemory.promise,Ue.allAssetsInMemory.promise_control.resolve())})).catch((e=>{throw Pe.err("Error in mono_download_assets: "+e),Xe(1,e),e}))}catch(e){throw Pe.err("Error in mono_download_assets: "+e),e}}}let ne=!1;function re(){if(ne)return;ne=!0;const e=Pe.config,t=[];if(e.assets)for(const t of e.assets)"object"!=typeof t&&Be(!1,`asset must be object, it was ${typeof t} : ${t}`),"string"!=typeof t.behavior&&Be(!1,"asset behavior must be known string"),"string"!=typeof t.name&&Be(!1,"asset name must be string"),t.resolvedUrl&&"string"!=typeof t.resolvedUrl&&Be(!1,"asset resolvedUrl could be string"),t.hash&&"string"!=typeof t.hash&&Be(!1,"asset resolvedUrl could be string"),t.pendingDownload&&"object"!=typeof t.pendingDownload&&Be(!1,"asset pendingDownload could be object"),t.isCore?$.push(t):z.push(t),X(t);else if(e.resources){const o=e.resources;o.wasmNative||Be(!1,"resources.wasmNative must be defined"),o.jsModuleNative||Be(!1,"resources.jsModuleNative must be defined"),o.jsModuleRuntime||Be(!1,"resources.jsModuleRuntime must be defined"),K(z,o.wasmNative,"dotnetwasm"),K(t,o.jsModuleNative,"js-module-native"),K(t,o.jsModuleRuntime,"js-module-runtime"),o.jsModuleDiagnostics&&K(t,o.jsModuleDiagnostics,"js-module-diagnostics");const n=(e,t,o)=>{const n=e;n.behavior=t,o?(n.isCore=!0,$.push(n)):z.push(n)};if(o.coreAssembly)for(let e=0;e<o.coreAssembly.length;e++)n(o.coreAssembly[e],"assembly",!0);if(o.assembly)for(let e=0;e<o.assembly.length;e++)n(o.assembly[e],"assembly",!o.coreAssembly);if(0!=e.debugLevel&&Pe.isDebuggingSupported()){if(o.corePdb)for(let e=0;e<o.corePdb.length;e++)n(o.corePdb[e],"pdb",!0);if(o.pdb)for(let e=0;e<o.pdb.length;e++)n(o.pdb[e],"pdb",!o.corePdb)}if(e.loadAllSatelliteResources&&o.satelliteResources)for(const e in o.satelliteResources)for(let t=0;t<o.satelliteResources[e].length;t++){const r=o.satelliteResources[e][t];r.culture=e,n(r,"resource",!o.coreAssembly)}if(o.coreVfs)for(let e=0;e<o.coreVfs.length;e++)n(o.coreVfs[e],"vfs",!0);if(o.vfs)for(let e=0;e<o.vfs.length;e++)n(o.vfs[e],"vfs",!o.coreVfs);const r=O(e);if(r&&o.icu)for(let e=0;e<o.icu.length;e++){const t=o.icu[e];t.name===r&&n(t,"icu",!1)}if(o.wasmSymbols)for(let e=0;e<o.wasmSymbols.length;e++)n(o.wasmSymbols[e],"symbols",!1)}if(e.appsettings)for(let t=0;t<e.appsettings.length;t++){const o=e.appsettings[t],n=he(o);"appsettings.json"!==n&&n!==`appsettings.${e.applicationEnvironment}.json`||z.push({name:o,behavior:"vfs",cache:"no-cache",useCredentials:!0})}e.assets=[...$,...z,...t]}async function ie(e){const t=await se(e);return await t.pendingDownloadInternal.response,t.buffer}async function se(e){try{return await ae(e)}catch(t){if(!Pe.enableDownloadRetry)throw t;if(Ie||Se)throw t;if(e.pendingDownload&&e.pendingDownloadInternal==e.pendingDownload)throw t;if(e.resolvedUrl&&-1!=e.resolvedUrl.indexOf("file://"))throw t;if(t&&404==t.status)throw t;e.pendingDownloadInternal=void 0,await Pe.allDownloadsQueued.promise;try{return Pe.diagnosticTracing&&b(`Retrying download '${e.name}'`),await ae(e)}catch(t){return e.pendingDownloadInternal=void 0,await new Promise((e=>globalThis.setTimeout(e,100))),Pe.diagnosticTracing&&b(`Retrying download (2) '${e.name}' after delay`),await ae(e)}}}async function ae(e){for(;L;)await L.promise;try{++N,N==Pe.maxParallelDownloads&&(Pe.diagnosticTracing&&b("Throttling further parallel downloads"),L=i());const t=await async function(e){if(e.pendingDownload&&(e.pendingDownloadInternal=e.pendingDownload),e.pendingDownloadInternal&&e.pendingDownloadInternal.response)return e.pendingDownloadInternal.response;if(e.buffer){const t=await e.buffer;return e.resolvedUrl||(e.resolvedUrl="undefined://"+e.name),e.pendingDownloadInternal={url:e.resolvedUrl,name:e.name,response:Promise.resolve({ok:!0,arrayBuffer:()=>t,json:()=>JSON.parse(new TextDecoder("utf-8").decode(t)),text:()=>{throw new Error("NotImplementedException")},headers:{get:()=>{}}})},e.pendingDownloadInternal.response}const t=e.loadRemote&&Pe.config.remoteSources?Pe.config.remoteSources:[""];let o;for(let n of t){n=n.trim(),"./"===n&&(n="");const t=le(e,n);e.name===t?Pe.diagnosticTracing&&b(`Attempting to download '${t}'`):Pe.diagnosticTracing&&b(`Attempting to download '${t}' for ${e.name}`);try{e.resolvedUrl=t;const n=fe(e);if(e.pendingDownloadInternal=n,o=await n.response,!o||!o.ok)continue;return o}catch(e){o||(o={ok:!1,url:t,status:0,statusText:""+e});continue}}const n=e.isOptional||e.name.match(/\.pdb$/)&&Pe.config.ignorePdbLoadErrors;if(o||Be(!1,`Response undefined ${e.name}`),!n){const t=new Error(`download '${o.url}' for ${e.name} failed ${o.status} ${o.statusText}`);throw t.status=o.status,t}y(`optional download '${o.url}' for ${e.name} failed ${o.status} ${o.statusText}`)}(e);return t?(J[e.behavior]||(e.buffer=await t.arrayBuffer(),++Pe.actual_downloaded_assets_count),e):e}finally{if(--N,L&&N==Pe.maxParallelDownloads-1){Pe.diagnosticTracing&&b("Resuming more parallel downloads");const e=L;L=void 0,e.promise_control.resolve()}}}function le(e,t){let o;return null==t&&Be(!1,`sourcePrefix must be provided for ${e.name}`),e.resolvedUrl?o=e.resolvedUrl:(o=""===t?"assembly"===e.behavior||"pdb"===e.behavior?e.name:"resource"===e.behavior&&e.culture&&""!==e.culture?`${e.culture}/${e.name}`:e.name:t+e.name,o=ce(Pe.locateFile(o),e.behavior)),o&&"string"==typeof o||Be(!1,"attemptUrl need to be path or url string"),o}function ce(e,t){return Pe.modulesUniqueQuery&&q[t]&&(e+=Pe.modulesUniqueQuery),e}let de=0;const ue=new Set;function fe(e){try{e.resolvedUrl||Be(!1,"Request's resolvedUrl must be set");const t=function(e){let t=e.resolvedUrl;if(Pe.loadBootResource){const o=ge(e);if(o instanceof Promise)return o;"string"==typeof o&&(t=o)}const o={};return e.cache?o.cache=e.cache:Pe.config.disableNoCacheFetch||(o.cache="no-cache"),e.useCredentials?o.credentials="include":!Pe.config.disableIntegrityCheck&&e.hash&&(o.integrity=e.hash),Pe.fetch_like(t,o)}(e),o={name:e.name,url:e.resolvedUrl,response:t};return ue.add(e.name),o.response.then((()=>{"assembly"==e.behavior&&Pe.loadedAssemblies.push(e.name),de++,Pe.onDownloadResourceProgress&&Pe.onDownloadResourceProgress(de,ue.size)})),o}catch(t){const o={ok:!1,url:e.resolvedUrl,status:500,statusText:"ERR29: "+t,arrayBuffer:()=>{throw t},json:()=>{throw t}};return{name:e.name,url:e.resolvedUrl,response:Promise.resolve(o)}}}const me={resource:"assembly",assembly:"assembly",pdb:"pdb",icu:"globalization",vfs:"configuration",manifest:"manifest",dotnetwasm:"dotnetwasm","js-module-dotnet":"dotnetjs","js-module-native":"dotnetjs","js-module-runtime":"dotnetjs","js-module-threads":"dotnetjs"};function ge(e){var t;if(Pe.loadBootResource){const o=null!==(t=e.hash)&&void 0!==t?t:"",n=e.resolvedUrl,r=me[e.behavior];if(r){const t=Pe.loadBootResource(r,e.name,n,o,e.behavior);return"string"==typeof t?I(t):t}}}function pe(e){e.pendingDownloadInternal=null,e.pendingDownload=null,e.buffer=null,e.moduleExports=null}function he(e){let t=e.lastIndexOf("/");return t>=0&&t++,e.substring(t)}async function we(e){e&&await Promise.all((null!=e?e:[]).map((e=>async function(e){try{const t=e.name;if(!e.moduleExports){const o=ce(Pe.locateFile(t),"js-module-library-initializer");Pe.diagnosticTracing&&b(`Attempting to import '${o}' for ${e}`),e.moduleExports=await import(/*! webpackIgnore: true */o)}Pe.libraryInitializers.push({scriptName:t,exports:e.moduleExports})}catch(t){E(`Failed to import library initializer '${e}': ${t}`)}}(e))))}async function be(e,t){if(!Pe.libraryInitializers)return;const o=[];for(let n=0;n<Pe.libraryInitializers.length;n++){const r=Pe.libraryInitializers[n];r.exports[e]&&o.push(ye(r.scriptName,e,(()=>r.exports[e](...t))))}await Promise.all(o)}async function ye(e,t,o){try{await o()}catch(o){throw E(`Failed to invoke '${t}' on library initializer '${e}': ${o}`),Xe(1,o),o}}function ve(e,t){if(e===t)return e;const o={...t};return void 0!==o.assets&&o.assets!==e.assets&&(o.assets=[...e.assets||[],...o.assets||[]]),void 0!==o.resources&&(o.resources=_e(e.resources||{assembly:[],jsModuleNative:[],jsModuleRuntime:[],wasmNative:[]},o.resources)),void 0!==o.environmentVariables&&(o.environmentVariables={...e.environmentVariables||{},...o.environmentVariables||{}}),void 0!==o.runtimeOptions&&o.runtimeOptions!==e.runtimeOptions&&(o.runtimeOptions=[...e.runtimeOptions||[],...o.runtimeOptions||[]]),Object.assign(e,o)}function Ee(e,t){if(e===t)return e;const o={...t};return o.config&&(e.config||(e.config={}),o.config=ve(e.config,o.config)),Object.assign(e,o)}function _e(e,t){if(e===t)return e;const o={...t};return void 0!==o.coreAssembly&&(o.coreAssembly=[...e.coreAssembly||[],...o.coreAssembly||[]]),void 0!==o.assembly&&(o.assembly=[...e.assembly||[],...o.assembly||[]]),void 0!==o.lazyAssembly&&(o.lazyAssembly=[...e.lazyAssembly||[],...o.lazyAssembly||[]]),void 0!==o.corePdb&&(o.corePdb=[...e.corePdb||[],...o.corePdb||[]]),void 0!==o.pdb&&(o.pdb=[...e.pdb||[],...o.pdb||[]]),void 0!==o.jsModuleWorker&&(o.jsModuleWorker=[...e.jsModuleWorker||[],...o.jsModuleWorker||[]]),void 0!==o.jsModuleNative&&(o.jsModuleNative=[...e.jsModuleNative||[],...o.jsModuleNative||[]]),void 0!==o.jsModuleDiagnostics&&(o.jsModuleDiagnostics=[...e.jsModuleDiagnostics||[],...o.jsModuleDiagnostics||[]]),void 0!==o.jsModuleRuntime&&(o.jsModuleRuntime=[...e.jsModuleRuntime||[],...o.jsModuleRuntime||[]]),void 0!==o.wasmSymbols&&(o.wasmSymbols=[...e.wasmSymbols||[],...o.wasmSymbols||[]]),void 0!==o.wasmNative&&(o.wasmNative=[...e.wasmNative||[],...o.wasmNative||[]]),void 0!==o.icu&&(o.icu=[...e.icu||[],...o.icu||[]]),void 0!==o.satelliteResources&&(o.satelliteResources=function(e,t){if(e===t)return e;for(const o in t)e[o]=[...e[o]||[],...t[o]||[]];return e}(e.satelliteResources||{},o.satelliteResources||{})),void 0!==o.modulesAfterConfigLoaded&&(o.modulesAfterConfigLoaded=[...e.modulesAfterConfigLoaded||[],...o.modulesAfterConfigLoaded||[]]),void 0!==o.modulesAfterRuntimeReady&&(o.modulesAfterRuntimeReady=[...e.modulesAfterRuntimeReady||[],...o.modulesAfterRuntimeReady||[]]),void 0!==o.extensions&&(o.extensions={...e.extensions||{},...o.extensions||{}}),void 0!==o.vfs&&(o.vfs=[...e.vfs||[],...o.vfs||[]]),Object.assign(e,o)}function xe(){const e=Pe.config;if(e.environmentVariables=e.environmentVariables||{},e.runtimeOptions=e.runtimeOptions||[],e.resources=e.resources||{assembly:[],jsModuleNative:[],jsModuleWorker:[],jsModuleRuntime:[],wasmNative:[],vfs:[],satelliteResources:{}},e.assets){Pe.diagnosticTracing&&b("config.assets is deprecated, use config.resources instead");for(const t of e.assets){const o={};switch(t.behavior){case"assembly":o.assembly=[t];break;case"pdb":o.pdb=[t];break;case"resource":o.satelliteResources={},o.satelliteResources[t.culture]=[t];break;case"icu":o.icu=[t];break;case"symbols":o.wasmSymbols=[t];break;case"vfs":o.vfs=[t];break;case"dotnetwasm":o.wasmNative=[t];break;case"js-module-threads":o.jsModuleWorker=[t];break;case"js-module-runtime":o.jsModuleRuntime=[t];break;case"js-module-native":o.jsModuleNative=[t];break;case"js-module-diagnostics":o.jsModuleDiagnostics=[t];break;case"js-module-dotnet":break;default:throw new Error(`Unexpected behavior ${t.behavior} of asset ${t.name}`)}_e(e.resources,o)}}e.debugLevel,e.applicationEnvironment||(e.applicationEnvironment="Production"),e.applicationCulture&&(e.environmentVariables.LANG=`${e.applicationCulture}.UTF-8`),Ue.diagnosticTracing=Pe.diagnosticTracing=!!e.diagnosticTracing,Ue.waitForDebugger=e.waitForDebugger,Pe.maxParallelDownloads=e.maxParallelDownloads||Pe.maxParallelDownloads,Pe.enableDownloadRetry=void 0!==e.enableDownloadRetry?e.enableDownloadRetry:Pe.enableDownloadRetry}let je=!1;async function Re(e){var t;if(je)return void await Pe.afterConfigLoaded.promise;let o;try{if(e.configSrc||Pe.config&&0!==Object.keys(Pe.config).length&&(Pe.config.assets||Pe.config.resources)||(e.configSrc="dotnet.boot.js"),o=e.configSrc,je=!0,o&&(Pe.diagnosticTracing&&b("mono_wasm_load_config"),await async function(e){const t=e.configSrc,o=Pe.locateFile(t);let n=null;void 0!==Pe.loadBootResource&&(n=Pe.loadBootResource("manifest",t,o,"","manifest"));let r,i=null;if(n)if("string"==typeof n)n.includes(".json")?(i=await s(I(n)),r=await Ae(i)):r=(await import(I(n))).config;else{const e=await n;"function"==typeof e.json?(i=e,r=await Ae(i)):r=e.config}else o.includes(".json")?(i=await s(ce(o,"manifest")),r=await Ae(i)):r=(await import(ce(o,"manifest"))).config;function s(e){return Pe.fetch_like(e,{method:"GET",credentials:"include",cache:"no-cache"})}Pe.config.applicationEnvironment&&(r.applicationEnvironment=Pe.config.applicationEnvironment),ve(Pe.config,r)}(e)),xe(),await we(null===(t=Pe.config.resources)||void 0===t?void 0:t.modulesAfterConfigLoaded),await be("onRuntimeConfigLoaded",[Pe.config]),e.onConfigLoaded)try{await e.onConfigLoaded(Pe.config,Le),xe()}catch(e){throw _("onConfigLoaded() failed",e),e}xe(),Pe.afterConfigLoaded.promise_control.resolve(Pe.config)}catch(t){const n=`Failed to load config file ${o} ${t} ${null==t?void 0:t.stack}`;throw Pe.config=e.config=Object.assign(Pe.config,{message:n,error:t,isError:!0}),Xe(1,new Error(n)),t}}function Te(){return!!globalThis.navigator&&(Pe.isChromium||Pe.isFirefox)}async function Ae(e){const t=Pe.config,o=await e.json();t.applicationEnvironment||o.applicationEnvironment||(o.applicationEnvironment=e.headers.get("Blazor-Environment")||e.headers.get("DotNet-Environment")||void 0),o.environmentVariables||(o.environmentVariables={});const n=e.headers.get("DOTNET-MODIFIABLE-ASSEMBLIES");n&&(o.environmentVariables.DOTNET_MODIFIABLE_ASSEMBLIES=n);const r=e.headers.get("ASPNETCORE-BROWSER-TOOLS");return r&&(o.environmentVariables.__ASPNETCORE_BROWSER_TOOLS=r),o}"function"!=typeof importScripts||globalThis.onmessage||(globalThis.dotnetSidecar=!0);const Se="object"==typeof process&&"object"==typeof process.versions&&"string"==typeof process.versions.node,De="function"==typeof importScripts,Oe=De&&"undefined"!=typeof dotnetSidecar,Ce=De&&!Oe,ke="object"==typeof window||De&&!Se,Ie=!ke&&!Se;let Ue={},Pe={},Me={},Le={},Ne={},$e=!1;const ze={},We={config:ze},Fe={mono:{},binding:{},internal:Ne,module:We,loaderHelpers:Pe,runtimeHelpers:Ue,diagnosticHelpers:Me,api:Le};function Be(e,t){if(e)return;const o="Assert failed: "+("function"==typeof t?t():t),n=new Error(o);_(o,n),Ue.nativeAbort(n)}function Ve(){return void 0!==Pe.exitCode}function qe(){return Ue.runtimeReady&&!Ve()}function He(){Ve()&&Be(!1,`.NET runtime already exited with ${Pe.exitCode} ${Pe.exitReason}. You can use runtime.runMain() which doesn't exit the runtime.`),Ue.runtimeReady||Be(!1,".NET runtime didn't start yet. Please call dotnet.create() first.")}function Je(){ke&&(globalThis.addEventListener("unhandledrejection",et),globalThis.addEventListener("error",tt))}let Ze,Qe;function Ge(e){Qe&&Qe(e),Xe(e,Pe.exitReason)}function Ke(e){Ze&&Ze(e||Pe.exitReason),Xe(1,e||Pe.exitReason)}function Xe(t,o){var n,r;const i=o&&"object"==typeof o;t=i&&"number"==typeof o.status?o.status:void 0===t?-1:t;const s=i&&"string"==typeof o.message?o.message:""+o;(o=i?o:Ue.ExitStatus?function(e,t){const o=new Ue.ExitStatus(e);return o.message=t,o.toString=()=>t,o}(t,s):new Error("Exit with code "+t+" "+s)).status=t,o.message||(o.message=s);const a=""+(o.stack||(new Error).stack);try{Object.defineProperty(o,"stack",{get:()=>a})}catch(e){}const l=!!o.silent;if(o.silent=!0,Ve())Pe.diagnosticTracing&&b("mono_exit called after exit");else{try{We.onAbort==Ke&&(We.onAbort=Ze),We.onExit==Ge&&(We.onExit=Qe),ke&&(globalThis.removeEventListener("unhandledrejection",et),globalThis.removeEventListener("error",tt)),Ue.runtimeReady?(Ue.jiterpreter_dump_stats&&Ue.jiterpreter_dump_stats(!1),0===t&&(null===(n=Pe.config)||void 0===n?void 0:n.interopCleanupOnExit)&&Ue.forceDisposeProxies(!0,!0),e&&0!==t&&(null===(r=Pe.config)||void 0===r||r.dumpThreadsOnNonZeroExit)):(Pe.diagnosticTracing&&b(`abort_startup, reason: ${o}`),function(e){Pe.allDownloadsQueued.promise_control.reject(e),Pe.allDownloadsFinished.promise_control.reject(e),Pe.afterConfigLoaded.promise_control.reject(e),Pe.wasmCompilePromise.promise_control.reject(e),Pe.runtimeModuleLoaded.promise_control.reject(e),Ue.dotnetReady&&(Ue.dotnetReady.promise_control.reject(e),Ue.afterInstantiateWasm.promise_control.reject(e),Ue.beforePreInit.promise_control.reject(e),Ue.afterPreInit.promise_control.reject(e),Ue.afterPreRun.promise_control.reject(e),Ue.beforeOnRuntimeInitialized.promise_control.reject(e),Ue.afterOnRuntimeInitialized.promise_control.reject(e),Ue.afterPostRun.promise_control.reject(e))}(o))}catch(e){E("mono_exit A failed",e)}try{l||(function(e,t){if(0!==e&&t){const e=Ue.ExitStatus&&t instanceof Ue.ExitStatus?b:_;"string"==typeof t?e(t):(void 0===t.stack&&(t.stack=(new Error).stack+""),t.message?e(Ue.stringify_as_error_with_stack?Ue.stringify_as_error_with_stack(t.message+"\n"+t.stack):t.message+"\n"+t.stack):e(JSON.stringify(t)))}!Ce&&Pe.config&&(Pe.config.logExitCode?Pe.config.forwardConsoleLogsToWS?R("WASM EXIT "+e):v("WASM EXIT "+e):Pe.config.forwardConsoleLogsToWS&&R())}(t,o),function(e){if(ke&&!Ce&&Pe.config&&Pe.config.appendElementOnExit&&document){const t=document.createElement("label");t.id="tests_done",0!==e&&(t.style.background="red"),t.innerHTML=""+e,document.body.appendChild(t)}}(t))}catch(e){E("mono_exit B failed",e)}Pe.exitCode=t,Pe.exitReason||(Pe.exitReason=o),!Ce&&Ue.runtimeReady&&We.runtimeKeepalivePop()}if(Pe.config&&Pe.config.asyncFlushOnExit&&0===t)throw(async()=>{try{await async function(){try{const e=await import(/*! webpackIgnore: true */"process"),t=e=>new Promise(((t,o)=>{e.on("error",o),e.end("","utf8",t)})),o=t(e.stderr),n=t(e.stdout);let r;const i=new Promise((e=>{r=setTimeout((()=>e("timeout")),1e3)}));await Promise.race([Promise.all([n,o]),i]),clearTimeout(r)}catch(e){_(`flushing std* streams failed: ${e}`)}}()}finally{Ye(t,o)}})(),o;Ye(t,o)}function Ye(e,t){if(Ue.runtimeReady&&Ue.nativeExit)try{Ue.nativeExit(e)}catch(e){!Ue.ExitStatus||e instanceof Ue.ExitStatus||E("set_exit_code_and_quit_now failed: "+e.toString())}if(0!==e||!ke)throw Se&&Ne.process?Ne.process.exit(e):Ue.quit&&Ue.quit(e,t),t}function et(e){ot(e,e.reason,"rejection")}function tt(e){ot(e,e.error,"error")}function ot(e,t,o){e.preventDefault();try{t||(t=new Error("Unhandled "+o)),void 0===t.stack&&(t.stack=(new Error).stack),t.stack=t.stack+"",t.silent||(_("Unhandled error:",t),Xe(1,t))}catch(e){}}!function(e){if($e)throw new Error("Loader module already loaded");$e=!0,Ue=e.runtimeHelpers,Pe=e.loaderHelpers,Me=e.diagnosticHelpers,Le=e.api,Ne=e.internal,Object.assign(Le,{INTERNAL:Ne,invokeLibraryInitializers:be}),Object.assign(e.module,{config:ve(ze,{environmentVariables:{}})});const r={mono_wasm_bindings_is_ready:!1,config:e.module.config,diagnosticTracing:!1,nativeAbort:e=>{throw e||new Error("abort")},nativeExit:e=>{throw new Error("exit:"+e)}},l={gitHash:"c2435c3e0f46de784341ac3ed62863ce77e117b4",config:e.module.config,diagnosticTracing:!1,maxParallelDownloads:16,enableDownloadRetry:!0,_loaded_files:[],loadedFiles:[],loadedAssemblies:[],libraryInitializers:[],workerNextNumber:1,actual_downloaded_assets_count:0,actual_instantiated_assets_count:0,expected_downloaded_assets_count:0,expected_instantiated_assets_count:0,afterConfigLoaded:i(),allDownloadsQueued:i(),allDownloadsFinished:i(),wasmCompilePromise:i(),runtimeModuleLoaded:i(),loadingWorkers:i(),is_exited:Ve,is_runtime_running:qe,assert_runtime_running:He,mono_exit:Xe,createPromiseController:i,getPromiseController:s,assertIsControllablePromise:a,mono_download_assets:oe,resolve_single_asset_path:ee,setup_proxy_console:j,set_thread_prefix:w,installUnhandledErrorHandler:Je,retrieve_asset_download:ie,invokeLibraryInitializers:be,isDebuggingSupported:Te,exceptions:t,simd:n,relaxedSimd:o};Object.assign(Ue,r),Object.assign(Pe,l)}(Fe);let nt,rt,it,st=!1,at=!1;async function lt(e){if(!at){if(at=!0,ke&&Pe.config.forwardConsoleLogsToWS&&void 0!==globalThis.WebSocket&&j("main",globalThis.console,globalThis.location.origin),We||Be(!1,"Null moduleConfig"),Pe.config||Be(!1,"Null moduleConfig.config"),"function"==typeof e){const t=e(Fe.api);if(t.ready)throw new Error("Module.ready couldn't be redefined.");Object.assign(We,t),Ee(We,t)}else{if("object"!=typeof e)throw new Error("Can't use moduleFactory callback of createDotnetRuntime function.");Ee(We,e)}await async function(e){if(Se){const e=await import(/*! webpackIgnore: true */"process"),t=14;if(e.versions.node.split(".")[0]<t)throw new Error(`NodeJS at '${e.execPath}' has too low version '${e.versions.node}', please use at least ${t}. See also https://aka.ms/dotnet-wasm-features`)}const t=/*! webpackIgnore: true */import.meta.url,o=t.indexOf("?");var n;if(o>0&&(Pe.modulesUniqueQuery=t.substring(o)),Pe.scriptUrl=t.replace(/\\/g,"/").replace(/[?#].*/,""),Pe.scriptDirectory=(n=Pe.scriptUrl).slice(0,n.lastIndexOf("/"))+"/",Pe.locateFile=e=>"URL"in globalThis&&globalThis.URL!==C?new URL(e,Pe.scriptDirectory).toString():M(e)?e:Pe.scriptDirectory+e,Pe.fetch_like=k,Pe.out=console.log,Pe.err=console.error,Pe.onDownloadResourceProgress=e.onDownloadResourceProgress,ke&&globalThis.navigator){const e=globalThis.navigator,t=e.userAgentData&&e.userAgentData.brands;t&&t.length>0?Pe.isChromium=t.some((e=>"Google Chrome"===e.brand||"Microsoft Edge"===e.brand||"Chromium"===e.brand)):e.userAgent&&(Pe.isChromium=e.userAgent.includes("Chrome"),Pe.isFirefox=e.userAgent.includes("Firefox"))}Ne.require=Se?await import(/*! webpackIgnore: true */"module").then((e=>e.createRequire(/*! webpackIgnore: true */import.meta.url))):Promise.resolve((()=>{throw new Error("require not supported")})),void 0===globalThis.URL&&(globalThis.URL=C)}(We)}}async function ct(e){return await lt(e),Ze=We.onAbort,Qe=We.onExit,We.onAbort=Ke,We.onExit=Ge,We.ENVIRONMENT_IS_PTHREAD?async function(){(function(){const e=new MessageChannel,t=e.port1,o=e.port2;t.addEventListener("message",(e=>{var n,r;n=JSON.parse(e.data.config),r=JSON.parse(e.data.monoThreadInfo),st?Pe.diagnosticTracing&&b("mono config already received"):(ve(Pe.config,n),Ue.monoThreadInfo=r,xe(),Pe.diagnosticTracing&&b("mono config received"),st=!0,Pe.afterConfigLoaded.promise_control.resolve(Pe.config),ke&&n.forwardConsoleLogsToWS&&void 0!==globalThis.WebSocket&&Pe.setup_proxy_console("worker-idle",console,globalThis.location.origin)),t.close(),o.close()}),{once:!0}),t.start(),self.postMessage({[l]:{monoCmd:"preload",port:o}},[o])})(),await Pe.afterConfigLoaded.promise,function(){const e=Pe.config;e.assets||Be(!1,"config.assets must be defined");for(const t of e.assets)X(t),Q[t.behavior]&&z.push(t)}(),setTimeout((async()=>{try{await oe()}catch(e){Xe(1,e)}}),0);const e=dt(),t=await Promise.all(e);return await ut(t),We}():async function(){var e;await Re(We),re();const t=dt();(async function(){try{const e=ee("dotnetwasm");await se(e),e&&e.pendingDownloadInternal&&e.pendingDownloadInternal.response||Be(!1,"Can't load dotnet.native.wasm");const t=await e.pendingDownloadInternal.response,o=t.headers&&t.headers.get?t.headers.get("Content-Type"):void 0;let n;if("function"==typeof WebAssembly.compileStreaming&&"application/wasm"===o)n=await WebAssembly.compileStreaming(t);else{ke&&"application/wasm"!==o&&E('WebAssembly resource does not have the expected content type "application/wasm", so falling back to slower ArrayBuffer instantiation.');const e=await t.arrayBuffer();Pe.diagnosticTracing&&b("instantiate_wasm_module buffered"),n=Ie?await Promise.resolve(new WebAssembly.Module(e)):await WebAssembly.compile(e)}e.pendingDownloadInternal=null,e.pendingDownload=null,e.buffer=null,e.moduleExports=null,Pe.wasmCompilePromise.promise_control.resolve(n)}catch(e){Pe.wasmCompilePromise.promise_control.reject(e)}})(),setTimeout((async()=>{try{D(),await oe()}catch(e){Xe(1,e)}}),0);const o=await Promise.all(t);return await ut(o),await Ue.dotnetReady.promise,await we(null===(e=Pe.config.resources)||void 0===e?void 0:e.modulesAfterRuntimeReady),await be("onRuntimeReady",[Fe.api]),Le}()}function dt(){const e=ee("js-module-runtime"),t=ee("js-module-native");if(nt&&rt)return[nt,rt,it];"object"==typeof e.moduleExports?nt=e.moduleExports:(Pe.diagnosticTracing&&b(`Attempting to import '${e.resolvedUrl}' for ${e.name}`),nt=import(/*! webpackIgnore: true */e.resolvedUrl)),"object"==typeof t.moduleExports?rt=t.moduleExports:(Pe.diagnosticTracing&&b(`Attempting to import '${t.resolvedUrl}' for ${t.name}`),rt=import(/*! webpackIgnore: true */t.resolvedUrl));const o=Y("js-module-diagnostics");return o&&("object"==typeof o.moduleExports?it=o.moduleExports:(Pe.diagnosticTracing&&b(`Attempting to import '${o.resolvedUrl}' for ${o.name}`),it=import(/*! webpackIgnore: true */o.resolvedUrl))),[nt,rt,it]}async function ut(e){const{initializeExports:t,initializeReplacements:o,configureRuntimeStartup:n,configureEmscriptenStartup:r,configureWorkerStartup:i,setRuntimeGlobals:s,passEmscriptenInternals:a}=e[0],{default:l}=e[1],c=e[2];s(Fe),t(Fe),c&&c.setRuntimeGlobals(Fe),await n(We),Pe.runtimeModuleLoaded.promise_control.resolve(),l((e=>(Object.assign(We,{ready:e.ready,__dotnet_runtime:{initializeReplacements:o,configureEmscriptenStartup:r,configureWorkerStartup:i,passEmscriptenInternals:a}}),We))).catch((e=>{if(e.message&&e.message.toLowerCase().includes("out of memory"))throw new Error(".NET runtime has failed to start, because too much memory was requested. Please decrease the memory by adjusting EmccMaximumHeapSize. See also https://aka.ms/dotnet-wasm-features");throw e}))}const ft=new class{withModuleConfig(e){try{return Ee(We,e),this}catch(e){throw Xe(1,e),e}}withOnConfigLoaded(e){try{return Ee(We,{onConfigLoaded:e}),this}catch(e){throw Xe(1,e),e}}withConsoleForwarding(){try{return ve(ze,{forwardConsoleLogsToWS:!0}),this}catch(e){throw Xe(1,e),e}}withExitOnUnhandledError(){try{return ve(ze,{exitOnUnhandledError:!0}),Je(),this}catch(e){throw Xe(1,e),e}}withAsyncFlushOnExit(){try{return ve(ze,{asyncFlushOnExit:!0}),this}catch(e){throw Xe(1,e),e}}withExitCodeLogging(){try{return ve(ze,{logExitCode:!0}),this}catch(e){throw Xe(1,e),e}}withElementOnExit(){try{return ve(ze,{appendElementOnExit:!0}),this}catch(e){throw Xe(1,e),e}}withInteropCleanupOnExit(){try{return ve(ze,{interopCleanupOnExit:!0}),this}catch(e){throw Xe(1,e),e}}withDumpThreadsOnNonZeroExit(){try{return ve(ze,{dumpThreadsOnNonZeroExit:!0}),this}catch(e){throw Xe(1,e),e}}withWaitingForDebugger(e){try{return ve(ze,{waitForDebugger:e}),this}catch(e){throw Xe(1,e),e}}withInterpreterPgo(e,t){try{return ve(ze,{interpreterPgo:e,interpreterPgoSaveDelay:t}),ze.runtimeOptions?ze.runtimeOptions.push("--interp-pgo-recording"):ze.runtimeOptions=["--interp-pgo-recording"],this}catch(e){throw Xe(1,e),e}}withConfig(e){try{return ve(ze,e),this}catch(e){throw Xe(1,e),e}}withConfigSrc(e){try{return e&&"string"==typeof e||Be(!1,"must be file path or URL"),Ee(We,{configSrc:e}),this}catch(e){throw Xe(1,e),e}}withVirtualWorkingDirectory(e){try{return e&&"string"==typeof e||Be(!1,"must be directory path"),ve(ze,{virtualWorkingDirectory:e}),this}catch(e){throw Xe(1,e),e}}withEnvironmentVariable(e,t){try{const o={};return o[e]=t,ve(ze,{environmentVariables:o}),this}catch(e){throw Xe(1,e),e}}withEnvironmentVariables(e){try{return e&&"object"==typeof e||Be(!1,"must be dictionary object"),ve(ze,{environmentVariables:e}),this}catch(e){throw Xe(1,e),e}}withDiagnosticTracing(e){try{return"boolean"!=typeof e&&Be(!1,"must be boolean"),ve(ze,{diagnosticTracing:e}),this}catch(e){throw Xe(1,e),e}}withDebugging(e){try{return null!=e&&"number"==typeof e||Be(!1,"must be number"),ve(ze,{debugLevel:e}),this}catch(e){throw Xe(1,e),e}}withApplicationArguments(...e){try{return e&&Array.isArray(e)||Be(!1,"must be array of strings"),ve(ze,{applicationArguments:e}),this}catch(e){throw Xe(1,e),e}}withRuntimeOptions(e){try{return e&&Array.isArray(e)||Be(!1,"must be array of strings"),ze.runtimeOptions?ze.runtimeOptions.push(...e):ze.runtimeOptions=e,this}catch(e){throw Xe(1,e),e}}withMainAssembly(e){try{return ve(ze,{mainAssemblyName:e}),this}catch(e){throw Xe(1,e),e}}withApplicationArgumentsFromQuery(){try{if(!globalThis.window)throw new Error("Missing window to the query parameters from");if(void 0===globalThis.URLSearchParams)throw new Error("URLSearchParams is supported");const e=new URLSearchParams(globalThis.window.location.search).getAll("arg");return this.withApplicationArguments(...e)}catch(e){throw Xe(1,e),e}}withApplicationEnvironment(e){try{return ve(ze,{applicationEnvironment:e}),this}catch(e){throw Xe(1,e),e}}withApplicationCulture(e){try{return ve(ze,{applicationCulture:e}),this}catch(e){throw Xe(1,e),e}}withResourceLoader(e){try{return Pe.loadBootResource=e,this}catch(e){throw Xe(1,e),e}}async download(){try{await async function(){lt(We),await Re(We),re(),D(),oe(),await Pe.allDownloadsFinished.promise}()}catch(e){throw Xe(1,e),e}}async create(){try{return this.instance||(this.instance=await async function(){return await ct(We),Fe.api}()),this.instance}catch(e){throw Xe(1,e),e}}async run(){try{return We.config||Be(!1,"Null moduleConfig.config"),this.instance||await this.create(),this.instance.runMainAndExit()}catch(e){throw Xe(1,e),e}}},mt=Xe,gt=ct;Ie||"function"==typeof globalThis.URL||Be(!1,"This browser/engine doesn't support URL API. Please use a modern version. See also https://aka.ms/dotnet-wasm-features"),"function"!=typeof globalThis.BigInt64Array&&Be(!1,"This browser/engine doesn't support BigInt64Array API. Please use a modern version. See also https://aka.ms/dotnet-wasm-features"),ft.withConfig(/*json-start*/{
  "mainAssemblyName": "PanoramicData.Blazor.WebAssembly.Client",
  "resources": {
    "hash": "sha256-HTLpAQLDYMf7F1g199VqFjrEbXAG1TOWNmTANnfEJL0=",
    "jsModuleNative": [
      {
        "name": "dotnet.native.69poregybn.js"
      }
    ],
    "jsModuleRuntime": [
      {
        "name": "dotnet.runtime.q5rqv3xrhm.js"
      }
    ],
    "wasmNative": [
      {
        "name": "dotnet.native.2mv1pqdd2n.wasm",
        "integrity": "sha256-tjie09uavTiX1TnkP9OL+OcnxCOs44ff5Dgws1rnAnc=",
        "cache": "force-cache"
      }
    ],
    "icu": [
      {
        "virtualPath": "icudt_CJK.dat",
        "name": "icudt_CJK.tjcz0u77k5.dat",
        "integrity": "sha256-SZLtQnRc0JkwqHab0VUVP7T3uBPSeYzxzDnpxPpUnHk=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "icudt_EFIGS.dat",
        "name": "icudt_EFIGS.tptq2av103.dat",
        "integrity": "sha256-8fItetYY8kQ0ww6oxwTLiT3oXlBwHKumbeP2pRF4yTc=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "icudt_no_CJK.dat",
        "name": "icudt_no_CJK.lfu7j35m59.dat",
        "integrity": "sha256-L7sV7NEYP37/Qr2FPCePo5cJqRgTXRwGHuwF5Q+0Nfs=",
        "cache": "force-cache"
      }
    ],
    "coreAssembly": [
      {
        "virtualPath": "System.Runtime.InteropServices.JavaScript.wasm",
        "name": "System.Runtime.InteropServices.JavaScript.ktbegkrd8y.wasm",
        "integrity": "sha256-WAM5OMcJO64InQovrLZ46el9w7eLyMLnICLGedHyr8Q=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Private.CoreLib.wasm",
        "name": "System.Private.CoreLib.2ioz9wrlv6.wasm",
        "integrity": "sha256-NoDSW7vPpEPoKBYtMzDPKhFpsj2mrvRJFbI4KrrPvew=",
        "cache": "force-cache"
      }
    ],
    "assembly": [
      {
        "virtualPath": "BlazorMonaco.wasm",
        "name": "BlazorMonaco.98tzo6dc2v.wasm",
        "integrity": "sha256-R8FyBgovIXHO+Iv0CZyrVhS+T30U6xRRX4tNBwjDfI8=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "ExtendedNumerics.BigDecimal.wasm",
        "name": "ExtendedNumerics.BigDecimal.qpze61gi54.wasm",
        "integrity": "sha256-smmKue3+aKeYyZZgI/QPRM0UEvZ3x4ywdvs6LYL/Vfg=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "FluentValidation.wasm",
        "name": "FluentValidation.yuptatg8bv.wasm",
        "integrity": "sha256-1vkL1fNCyvLkWYtavCvKdUZS0BPY392ec7LutoPTps4=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Humanizer.wasm",
        "name": "Humanizer.l1tz1ujrse.wasm",
        "integrity": "sha256-N7kaAs9it1XVB6XTx31+dVGAU29AzWn3CV++JKJ36M8=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Markdig.wasm",
        "name": "Markdig.rwzcx7tleo.wasm",
        "integrity": "sha256-NXLgWnqkZizVTwXowikUaNTIoFUWRCwAYrP5yNrjZ8M=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.AspNetCore.Components.wasm",
        "name": "Microsoft.AspNetCore.Components.zq8639elmt.wasm",
        "integrity": "sha256-yzi5zQHuq1BXQd+hw7Vp1XXO+xoW2KTwvek0//eYgBU=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.AspNetCore.Components.Forms.wasm",
        "name": "Microsoft.AspNetCore.Components.Forms.bu9hhccp18.wasm",
        "integrity": "sha256-E62ZiRQeF6EqWRwORFWEdCqSB910+jhdffaWuLiu+LA=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.AspNetCore.Components.Web.wasm",
        "name": "Microsoft.AspNetCore.Components.Web.p081oesna2.wasm",
        "integrity": "sha256-2W483COvztYbQu+YubwJVpA0r5AFlpKpFKWrEgiqOjg=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.AspNetCore.Components.WebAssembly.wasm",
        "name": "Microsoft.AspNetCore.Components.WebAssembly.84qilbxykt.wasm",
        "integrity": "sha256-uCVa0h6PF14T9Ubyye8GQKK3PG+veXOsWln/dLHp1hU=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.AspNetCore.WebUtilities.wasm",
        "name": "Microsoft.AspNetCore.WebUtilities.aoy1uen2qs.wasm",
        "integrity": "sha256-NtSnbKKADerdxzEJatGRm6qlHw0aHN4VVwsD4YwUiCc=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Build.Framework.wasm",
        "name": "Microsoft.Build.Framework.z1two6g647.wasm",
        "integrity": "sha256-6TRbpMfIZY3A+ONJ7ShwVux0bwWnb2tFsFa06wZf3Uk=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Build.Utilities.Core.wasm",
        "name": "Microsoft.Build.Utilities.Core.x7uce0pv8i.wasm",
        "integrity": "sha256-/UGyFl8dZ1kJ2HhKC297xMFJiNVKeIm3bU6+xNqEfD4=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Configuration.wasm",
        "name": "Microsoft.Extensions.Configuration.2i9ccqld3q.wasm",
        "integrity": "sha256-5oDuhX6Uxlb7MGFUMi7qSe+tezr4as3YwYgoBQNkc/Y=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Configuration.Abstractions.wasm",
        "name": "Microsoft.Extensions.Configuration.Abstractions.5gu2i6uw7f.wasm",
        "integrity": "sha256-W+/xA9xTCZaOykNzKMwe3YDmkZNOPdJqb/E+VYPCk8w=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Configuration.Binder.wasm",
        "name": "Microsoft.Extensions.Configuration.Binder.ruz0wplp7h.wasm",
        "integrity": "sha256-fZa131sqgbyiL2D2x8miQpbpkjBM1ofiAtvp6/f3Qsk=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Configuration.Json.wasm",
        "name": "Microsoft.Extensions.Configuration.Json.dsinhvif0g.wasm",
        "integrity": "sha256-I9RchUtUIR389BC9FoYb+kicxmt1RhJha7WRlbfEbBQ=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.DependencyInjection.wasm",
        "name": "Microsoft.Extensions.DependencyInjection.5tn3twdons.wasm",
        "integrity": "sha256-SShfrUJLUGthGohbsZrISXzpK06sbKz0v/5HICTu23w=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.DependencyInjection.Abstractions.wasm",
        "name": "Microsoft.Extensions.DependencyInjection.Abstractions.93jl2buuyz.wasm",
        "integrity": "sha256-G/4aenFwg87hWAP0P5wVxeyiiwA8ihH40bXhPnLj0zg=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Logging.wasm",
        "name": "Microsoft.Extensions.Logging.wcqdsugkjr.wasm",
        "integrity": "sha256-sJrTLoCd36hWE1wSOxf7x050HDj0+7E0Ue8Ab+ozrRo=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Logging.Abstractions.wasm",
        "name": "Microsoft.Extensions.Logging.Abstractions.8u3nb0neny.wasm",
        "integrity": "sha256-ME4LkOvjmeEeJVf1LKaKDAEBcZFzd1xZltpYIDll6xM=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Logging.Configuration.wasm",
        "name": "Microsoft.Extensions.Logging.Configuration.ioew026qee.wasm",
        "integrity": "sha256-Ujq/INT0GewVKNT3/ztfWX7oZJbLd8mxjXB8xXlnZLw=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Logging.Console.wasm",
        "name": "Microsoft.Extensions.Logging.Console.x25x2fl3t9.wasm",
        "integrity": "sha256-B3q9v9VJSue5yJDMwaCgc0DbKv59yr3c3zC+4pQni7M=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Logging.TraceSource.wasm",
        "name": "Microsoft.Extensions.Logging.TraceSource.7ps6g6mg08.wasm",
        "integrity": "sha256-yx4+SrUfmAVkxjPPA13+1G9R/swCiZpqnGlGcZ7SI/c=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Options.wasm",
        "name": "Microsoft.Extensions.Options.hpxn5zhjfy.wasm",
        "integrity": "sha256-ZWvLi4uD1DhpugB3wdKLcFn2yjTRZKmZhg7SRHhNgxE=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Options.ConfigurationExtensions.wasm",
        "name": "Microsoft.Extensions.Options.ConfigurationExtensions.mfjpnx9hxo.wasm",
        "integrity": "sha256-opmpLGwC+QFzexuTcJvb8fAvElyG4Al1blZOBLTwkx0=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Primitives.wasm",
        "name": "Microsoft.Extensions.Primitives.r96a69h9yy.wasm",
        "integrity": "sha256-PxhbdNFLAYlhAHGu6Ux/SONQlsVI0ap5HupJGuv1Xc4=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.JSInterop.wasm",
        "name": "Microsoft.JSInterop.2o1spqoebo.wasm",
        "integrity": "sha256-jqea+X5H0XiNGsKyKuNHmPUlKUNH0+NDKVx8XXg+XHs=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.JSInterop.WebAssembly.wasm",
        "name": "Microsoft.JSInterop.WebAssembly.58jjh3bwur.wasm",
        "integrity": "sha256-FTVJx7gilBV/VWu+ibz7K8ueo7JUXPGj94aH0rf2FXQ=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.NET.StringTools.wasm",
        "name": "Microsoft.NET.StringTools.d4ivdki312.wasm",
        "integrity": "sha256-fUr7ysgYmUcXLndQOnYMguW9nkYBD6oJ8m1rTtyNzKc=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Win32.SystemEvents.wasm",
        "name": "Microsoft.Win32.SystemEvents.xtkozxv820.wasm",
        "integrity": "sha256-i2lXUYrTjcDpIQO/+vXkStHs2QDNfhxZtCrmoSbn9ZY=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "NCalc.Core.wasm",
        "name": "NCalc.Core.9d2qx83nyk.wasm",
        "integrity": "sha256-g6DDQoj2tBCksydRMEwYdV6Z0/1gGzUd+b5vgeWF7Ig=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "NCalc.Sync.wasm",
        "name": "NCalc.Sync.kdd73bt7a8.wasm",
        "integrity": "sha256-iEG2lVIuaJyxTpUZ8pWWnKGIIrGhql7pPhzOULkhbIk=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Newtonsoft.Json.wasm",
        "name": "Newtonsoft.Json.jcjjiqe038.wasm",
        "integrity": "sha256-s8KVuknfxWl1cuDvQM/OnpBfnpM1rxzvzq21S1cF36U=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "NuGet.Common.wasm",
        "name": "NuGet.Common.7okjqqu09f.wasm",
        "integrity": "sha256-Z9EFpSBAM9JZhkzdcaS6lbcmI7K1AbtF/aU0g42N0es=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "NuGet.Frameworks.wasm",
        "name": "NuGet.Frameworks.fgtshwxdkl.wasm",
        "integrity": "sha256-HwUvyIQQhM4gnxJ01D7qF/aS1bsO8xPSxFtLBVUk/rI=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "PanoramicData.DeepCloner.wasm",
        "name": "PanoramicData.DeepCloner.gciadpwkv3.wasm",
        "integrity": "sha256-oFNX/3BbjpzGt+fyaA6gN/VaV7RMrY946/9RN+AQUPM=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "PanoramicData.NCalcExtensions.wasm",
        "name": "PanoramicData.NCalcExtensions.1e7cb2a6bu.wasm",
        "integrity": "sha256-aQOXvrBOavQ9zSkChHvlHaGvW0COCwmRuxiZUiwQO0U=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Parlot.wasm",
        "name": "Parlot.1nryewifiw.wasm",
        "integrity": "sha256-be7aeIIeMapHcgEt12qeKu3vYhpzNeytX9hGvmqztDA=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Configuration.ConfigurationManager.wasm",
        "name": "System.Configuration.ConfigurationManager.4767g36hke.wasm",
        "integrity": "sha256-4EiIgjbnJ6Dx8BpGyZX/T3eNOKXWbkhkXkEoTC2kh0w=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Diagnostics.EventLog.wasm",
        "name": "System.Diagnostics.EventLog.plabvnx2s8.wasm",
        "integrity": "sha256-bdWAhzVPV7R2jGl6jGgkTxB8DO43EDuh8ZOV7QSiynI=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Drawing.Common.wasm",
        "name": "System.Drawing.Common.aw2xayg6sf.wasm",
        "integrity": "sha256-6h6gZVmZr+tgrRUlWzhvF24TKKxBiK/+RkztdQLDCGI=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Private.Windows.Core.wasm",
        "name": "System.Private.Windows.Core.15sbjvge5y.wasm",
        "integrity": "sha256-y02xDTHiRTLVTD5+0oAE6FGYA1JUezOIpqafIA6tEOE=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Private.Windows.GdiPlus.wasm",
        "name": "System.Private.Windows.GdiPlus.v1e6iva9lh.wasm",
        "integrity": "sha256-XWW0zELwvBzNAvlAbvYfpDIpNzk1+pVqdSImcJZgiTg=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Linq.Dynamic.Core.wasm",
        "name": "System.Linq.Dynamic.Core.yt3i0kvfqy.wasm",
        "integrity": "sha256-5mMcR6Lmx8cWJWZek6QwlfKvlcYMgwRBLA5/5KeAVes=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Security.Cryptography.ProtectedData.wasm",
        "name": "System.Security.Cryptography.ProtectedData.lqfmzyh7ta.wasm",
        "integrity": "sha256-02v8roui+7f6Snn5FDmsP3YCsgxNX3ITLVLhv2JyEp0=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "TimeZoneConverter.wasm",
        "name": "TimeZoneConverter.halbc9brmz.wasm",
        "integrity": "sha256-d//r0f3cQpD9eCyyNd4couPMBX58Ozltb/MEZbzmWoE=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.CSharp.wasm",
        "name": "Microsoft.CSharp.55pzwevgi4.wasm",
        "integrity": "sha256-9420pAJs4G6tFckc9PllkYmdqRSK1/UlrLCb0tAo0bQ=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Win32.Primitives.wasm",
        "name": "Microsoft.Win32.Primitives.at4tofna6k.wasm",
        "integrity": "sha256-Yp6F/C5xFSS+Z/idJ/8YQ6kM/46JHssrXe3uWkdrPxA=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Win32.Registry.wasm",
        "name": "Microsoft.Win32.Registry.dhblo8zxit.wasm",
        "integrity": "sha256-nyrJZpdLbg/Abk+c509OTHi4Ezuzg7FPNlgIFy86Ubw=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Collections.Concurrent.wasm",
        "name": "System.Collections.Concurrent.obqjw2o4w3.wasm",
        "integrity": "sha256-BPR1DbccBu19CmXc7Mj7/a8jZWcuI/xzDUf1ZJeU3vw=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Collections.Immutable.wasm",
        "name": "System.Collections.Immutable.hm62yhjk8s.wasm",
        "integrity": "sha256-CQThybK+X9BxtM10tEbJhrFXr28KFUWncObIoNkGde8=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Collections.NonGeneric.wasm",
        "name": "System.Collections.NonGeneric.ozbfev0lx3.wasm",
        "integrity": "sha256-dNTjSMKXzStzizOGzKluD9ZReZUXuc5irrEcF1hzWPA=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Collections.Specialized.wasm",
        "name": "System.Collections.Specialized.d76bnvggvz.wasm",
        "integrity": "sha256-KUcfv6Rvxw8SX8nTcSqS5g/mSXWDy+N9XY2gQGbrgyw=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Collections.wasm",
        "name": "System.Collections.ahvherzwf1.wasm",
        "integrity": "sha256-ngOtsh0+Mm1AD6C+tIzbgZ27eovLJ4E1h+ogMmnY+f4=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.ComponentModel.Annotations.wasm",
        "name": "System.ComponentModel.Annotations.umvj6swo0d.wasm",
        "integrity": "sha256-GiNyyNiChmG3gsGDUyk5KGhyD7B8aUNXauOauZZnVqE=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.ComponentModel.Primitives.wasm",
        "name": "System.ComponentModel.Primitives.1n5luub2di.wasm",
        "integrity": "sha256-M6opnhuEP5iC2Fz4/hEe/J3tI2eH6mignDK0eL1JpQA=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.ComponentModel.TypeConverter.wasm",
        "name": "System.ComponentModel.TypeConverter.art3trickd.wasm",
        "integrity": "sha256-ZmIrPcwv988MVapK/3Tq5dBl9K+RMk99njJ4K79gHTU=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.ComponentModel.wasm",
        "name": "System.ComponentModel.m3nwsthjaj.wasm",
        "integrity": "sha256-8jyOKB6r6mlasVCeBs25cPAxWo3OcNUDSLdSzAK/hC8=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Console.wasm",
        "name": "System.Console.3kkspl8wjl.wasm",
        "integrity": "sha256-7ftzjW6huxuw+r/YBQ6uyyNNfKTVgMJr8juw/25q4sk=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Core.wasm",
        "name": "System.Core.zcbadcxyey.wasm",
        "integrity": "sha256-A8XPVUtS87o3+N04hXe1wtLepz/jsoSDluJphOu99dg=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Data.Common.wasm",
        "name": "System.Data.Common.syrh8a6865.wasm",
        "integrity": "sha256-1HeKz7WMe98RsQQV7aXm3iie8KMe1n4/FlMhmKufBaw=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Diagnostics.DiagnosticSource.wasm",
        "name": "System.Diagnostics.DiagnosticSource.h7vy9xtysv.wasm",
        "integrity": "sha256-fe+/zEZ1UKB8T53/FfkVt6OQnAOq9ZH0XOZ65EhVG6U=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Diagnostics.Process.wasm",
        "name": "System.Diagnostics.Process.ntfzvy4f2j.wasm",
        "integrity": "sha256-24GN2x6GKJclouuLt5HLTkeYkbbErFWHedzijDwMuMw=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Diagnostics.StackTrace.wasm",
        "name": "System.Diagnostics.StackTrace.b0egdmab9p.wasm",
        "integrity": "sha256-FbAcpqa4jMzhIfA5TSja4aW3uCw9XYDTV67/auwAbsY=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Diagnostics.TextWriterTraceListener.wasm",
        "name": "System.Diagnostics.TextWriterTraceListener.9xfhyd60kf.wasm",
        "integrity": "sha256-3YCYhmpgALl8ie7D0Sn17VBH2ayChRncF/WfztkGi08=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Diagnostics.TraceSource.wasm",
        "name": "System.Diagnostics.TraceSource.3jaazmeg3w.wasm",
        "integrity": "sha256-z07MksDwURcLpc6VIKuV+l8mEaTTYlfZbCB6ynA4tJE=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Diagnostics.Tracing.wasm",
        "name": "System.Diagnostics.Tracing.1wa1tpoe76.wasm",
        "integrity": "sha256-n9G9evRtQj/SQRkmVKcgr7JvLsiO6A/Ymmrv+H0B/nU=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Drawing.Primitives.wasm",
        "name": "System.Drawing.Primitives.q882ameotm.wasm",
        "integrity": "sha256-xifGvowClFv3A2Bq6FaQPW/029Pq9fvM0b3BuNs6Wf4=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Drawing.wasm",
        "name": "System.Drawing.8wgi7x9zvd.wasm",
        "integrity": "sha256-+P6iv9BqBaSzIMcl/QoNjmkXV39NyvncqPXZ1Ss7398=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.IO.Compression.ZipFile.wasm",
        "name": "System.IO.Compression.ZipFile.ylbiypqrfe.wasm",
        "integrity": "sha256-ui7ydIBOelelaGoaqVMRFXsl+sTmyI08G1nGapmDtqQ=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.IO.Compression.wasm",
        "name": "System.IO.Compression.g0a952u5d0.wasm",
        "integrity": "sha256-ENhrgY5rQTC4/MCPHpD2us+uFnIcE5h8NjV27LNMraM=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.IO.MemoryMappedFiles.wasm",
        "name": "System.IO.MemoryMappedFiles.zn3zrwfe2r.wasm",
        "integrity": "sha256-sRfXsXtfMMTo2CLx4ZKDFwtkYvOe/IRL5Gwf4dsnXMk=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.IO.Pipelines.wasm",
        "name": "System.IO.Pipelines.orwcdf2bqi.wasm",
        "integrity": "sha256-5NlyiKMjQMJVLZ+E92VHzP4NXkvjJnT4LTZkWqm0KJc=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Linq.Expressions.wasm",
        "name": "System.Linq.Expressions.ktt8qu37u3.wasm",
        "integrity": "sha256-+Mq6q90dsf5E5DlgIsUXmZGwmDBHoSTxzdzHHMi+MOo=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Linq.Parallel.wasm",
        "name": "System.Linq.Parallel.5ibvpihq7h.wasm",
        "integrity": "sha256-A+Mr2C2u7MvTekyC5W3ZpslySeubiVu4JAih179tvwo=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Linq.Queryable.wasm",
        "name": "System.Linq.Queryable.l4mhnkdyn3.wasm",
        "integrity": "sha256-RSF6b2FxNUxnq0jqHG1IcPrSJLFTWW4ZVSNXTLSjyzI=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Linq.wasm",
        "name": "System.Linq.urhwtusvnq.wasm",
        "integrity": "sha256-BukDgxjLNkCWCFiO7cK+VPuGvGCrbrh2qB+n3TvIovo=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Memory.wasm",
        "name": "System.Memory.vdmvvd8fnr.wasm",
        "integrity": "sha256-E5YcrdRDr8hRzgGa33zvKxrsWwfnVThr4EZNp8sXaTg=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Net.Http.wasm",
        "name": "System.Net.Http.x8o87ie9cq.wasm",
        "integrity": "sha256-P8TPxm8hRKW7689W5pyF+UCj+xOkM2OOGBvnPIqgv/g=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Net.Primitives.wasm",
        "name": "System.Net.Primitives.rtv3zboioc.wasm",
        "integrity": "sha256-IS1F8gernr3O+ZlvSDMJ2C5/ydBbP2eFustERMXQJbc=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Net.WebClient.wasm",
        "name": "System.Net.WebClient.7b6cgiipjz.wasm",
        "integrity": "sha256-DV17KEsOHE+rD7UF8nszjbUkpD/i3ix9EpRG77In+/w=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Numerics.Vectors.wasm",
        "name": "System.Numerics.Vectors.lnt8u6ob9j.wasm",
        "integrity": "sha256-38HN1ZugBbOfqqw1A/W7qE5mFR5jwuBSFvELSA1Cpow=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.ObjectModel.wasm",
        "name": "System.ObjectModel.ln6z1u24vr.wasm",
        "integrity": "sha256-j/orGPp1WbDW9TRouvgYdbl3ncK+dWY428tyqKP02e0=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Private.DataContractSerialization.wasm",
        "name": "System.Private.DataContractSerialization.nrybnhtza5.wasm",
        "integrity": "sha256-w2+gKug9HnBbQsR+aUutqxHX9AerX1bf/5PoA3Cl3nI=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Private.Uri.wasm",
        "name": "System.Private.Uri.g6qswsrcrz.wasm",
        "integrity": "sha256-eMxE8vF63WvfsJLKdd82wGD5rm4LnhG550WxpOT5qfY=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Private.Xml.Linq.wasm",
        "name": "System.Private.Xml.Linq.chqbr00iyf.wasm",
        "integrity": "sha256-0NVKiQ9aUB9YbbX5BjLz7S8pj1gNuBdRJtfA02Y6fDQ=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Private.Xml.wasm",
        "name": "System.Private.Xml.xx3hb1znet.wasm",
        "integrity": "sha256-y1LGwAMCIuHfk0glyi9nT+LP3ET7eoZS7QAJ41UCA6k=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Reflection.Emit.ILGeneration.wasm",
        "name": "System.Reflection.Emit.ILGeneration.ozy0lcnioo.wasm",
        "integrity": "sha256-JIZ9bNsv8uFM3G446m3ojqqoIzOhtHFQTMX9xkl8ry8=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Reflection.Emit.Lightweight.wasm",
        "name": "System.Reflection.Emit.Lightweight.u1f06yqgqz.wasm",
        "integrity": "sha256-MnoV33rJKyM4ob2m+B4jDhQln2wUkc36gzI6zNKR+ps=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Reflection.Emit.wasm",
        "name": "System.Reflection.Emit.v95jx8t42v.wasm",
        "integrity": "sha256-c1IM3phfGVIm/I5OgIglGoGEcHWwHoKqwo8exqK9ckg=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Reflection.Metadata.wasm",
        "name": "System.Reflection.Metadata.imdhdtwevy.wasm",
        "integrity": "sha256-OqB+WrTc93l52mqzAYefDhfMIFe3OWabvWMJNEyG++k=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Reflection.Primitives.wasm",
        "name": "System.Reflection.Primitives.llud1kgopk.wasm",
        "integrity": "sha256-17JX0oFv8cWayUdsa7GbMVhDePcnuc6pT+LCbVzxOj8=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Runtime.InteropServices.wasm",
        "name": "System.Runtime.InteropServices.qvh1clk3ak.wasm",
        "integrity": "sha256-Ou9J5O983336/rJp6iYQcsYdJ6exj/vCsr/yfRGh0T8=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Runtime.Intrinsics.wasm",
        "name": "System.Runtime.Intrinsics.2g2ksybp3y.wasm",
        "integrity": "sha256-Yp0qyImyW6hc37px6R34DtGZtomyTsL7JmIYxEb2Wjc=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Runtime.Numerics.wasm",
        "name": "System.Runtime.Numerics.a3vnrx798r.wasm",
        "integrity": "sha256-JsyhJDR4SKF+8t1WX6p0AcXGyhBM2ErOq7oit1tzjLs=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Runtime.Serialization.Formatters.wasm",
        "name": "System.Runtime.Serialization.Formatters.skzw1yf2vn.wasm",
        "integrity": "sha256-5KqbOewBR6uoQiGalV2OoG2JobZsa9v8sJGfUI84A5Q=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Runtime.Serialization.Primitives.wasm",
        "name": "System.Runtime.Serialization.Primitives.mkqgpxq111.wasm",
        "integrity": "sha256-elipxlKd3KP3qgF4lsaQOf3TCKXS8A6H2hoY3QWi0L8=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Runtime.Serialization.Xml.wasm",
        "name": "System.Runtime.Serialization.Xml.42byvrzfku.wasm",
        "integrity": "sha256-hNiMEhZVyeishca5xe94NKpclmSLS6yZGdH+KTz7Tlg=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Runtime.wasm",
        "name": "System.Runtime.ayg7qgsg0v.wasm",
        "integrity": "sha256-kqsPQFAJERx9E/pPZBnI1rTtTidf6yRYlqqpAlVWtUQ=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Security.Cryptography.wasm",
        "name": "System.Security.Cryptography.l326qb61o5.wasm",
        "integrity": "sha256-IVaYIOgGmAhPAOxHq6UsgtGJx3kma8mrLI1lU7ELZPA=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Text.Encoding.CodePages.wasm",
        "name": "System.Text.Encoding.CodePages.l5ujomf79s.wasm",
        "integrity": "sha256-af/NOYzPM+AfT78Mhs8/vHcCwsrWbbIllujxhO41e2Q=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Text.Encoding.Extensions.wasm",
        "name": "System.Text.Encoding.Extensions.fi0dtt4puq.wasm",
        "integrity": "sha256-KWvELXLSU2vu2ftF3Ma5/7mx80+OBA8QOds1m6agZ1k=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Text.Encodings.Web.wasm",
        "name": "System.Text.Encodings.Web.58er5y1em3.wasm",
        "integrity": "sha256-xf1Vzl4tlK9rH/fwoZh89scyWTI0JGgsH3r3a0lYjXw=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Text.Json.wasm",
        "name": "System.Text.Json.nfdn5hp46o.wasm",
        "integrity": "sha256-PcOCyMGOpf7/Bc5sfcmeo02Y7iEiLsi8dIol/6Xlv7w=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Text.RegularExpressions.wasm",
        "name": "System.Text.RegularExpressions.t7gj65rn2v.wasm",
        "integrity": "sha256-2UIpO93oQ0xP2lwgK6KGTzFQPq7DKV9as5iCSBsD3jY=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Threading.Tasks.Parallel.wasm",
        "name": "System.Threading.Tasks.Parallel.8udaxaly9k.wasm",
        "integrity": "sha256-cKJWwpmGT32RDF/DdWFb8Lo4iat4v3BGZGkiGWfeEN4=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Threading.Thread.wasm",
        "name": "System.Threading.Thread.qps0iejx02.wasm",
        "integrity": "sha256-orQAgZhon+XYKe3FlLaXxc2jBPpStNI5ndu11jpzVlg=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Threading.wasm",
        "name": "System.Threading.y4yv0hweda.wasm",
        "integrity": "sha256-bt3FHT+ANIGsuqfamf+ncw3EYrfRctNEnqMCh09KCw0=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Web.HttpUtility.wasm",
        "name": "System.Web.HttpUtility.ew8g0w6phh.wasm",
        "integrity": "sha256-8k6M1PMn8sGArAAyjZTxzQLOqCWmQYAiyWne3Cwn75Y=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Xml.Linq.wasm",
        "name": "System.Xml.Linq.ysq4rzz6yg.wasm",
        "integrity": "sha256-sndSZhomaMgZJSV8YYnGu9JTyssQJWIyipDAPAgJbwQ=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Xml.ReaderWriter.wasm",
        "name": "System.Xml.ReaderWriter.a73jrrj3xr.wasm",
        "integrity": "sha256-CjK52EUKKuqNLsdzVdlP7yyn5FeYXJClZ0vTXXurF3g=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Xml.XDocument.wasm",
        "name": "System.Xml.XDocument.e3blplj2t4.wasm",
        "integrity": "sha256-aghnAJylb+A4gWiREF69ddjEeyOL1ft5DYGqnPsmscw=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Xml.XmlSerializer.wasm",
        "name": "System.Xml.XmlSerializer.nj1ei025cu.wasm",
        "integrity": "sha256-rU5P/lvPGE6hyTviuBA8ctx/sC1+I/LdTI7n+DL2pL4=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.wasm",
        "name": "System.deed694j9w.wasm",
        "integrity": "sha256-JRSnUKU4RCZCvwQXVaK87jabHPSnQK1daxG24xvCico=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "netstandard.wasm",
        "name": "netstandard.3a2ue9qa8y.wasm",
        "integrity": "sha256-9eYt7clmezPYQxTSSGvDQYZsyQFwP+ufu0sxaFED9LY=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "PanoramicData.Blazor.Demo.wasm",
        "name": "PanoramicData.Blazor.Demo.e3kzbnmlyn.wasm",
        "integrity": "sha256-epd8BtrRNxdaKWq6tYg44sUI1QOBDAn27RZcLxxeCxU=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "PanoramicData.Blazor.wasm",
        "name": "PanoramicData.Blazor.nahgy61h05.wasm",
        "integrity": "sha256-j0UY4WKId0YGdCneAAGdlW6pMMp7kAVwTBuNLwVVroU=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "PanoramicData.Blazor.WebAssembly.Client.wasm",
        "name": "PanoramicData.Blazor.WebAssembly.Client.mzevdibvw6.wasm",
        "integrity": "sha256-Xn1ldOfGNbqrbxgxLNdRFO3PYu9SXlXRA05ZTKBSoes=",
        "cache": "force-cache"
      }
    ]
  },
  "debugLevel": 0,
  "linkerEnabled": true,
  "globalizationMode": "sharded",
  "extensions": {
    "blazor": {}
  },
  "runtimeConfig": {
    "runtimeOptions": {
      "configProperties": {
        "Microsoft.AspNetCore.Components.Routing.RegexConstraintSupport": false,
        "Microsoft.Extensions.DependencyInjection.VerifyOpenGenericServiceTrimmability": true,
        "System.ComponentModel.DefaultValueAttribute.IsSupported": false,
        "System.ComponentModel.Design.IDesignerHost.IsSupported": false,
        "System.ComponentModel.TypeConverter.EnableUnsafeBinaryFormatterInDesigntimeLicenseContextSerialization": false,
        "System.ComponentModel.TypeDescriptor.IsComObjectDescriptorSupported": false,
        "System.Data.DataSet.XmlSerializationIsSupported": false,
        "System.Diagnostics.Debugger.IsSupported": false,
        "System.Diagnostics.Metrics.Meter.IsSupported": false,
        "System.Diagnostics.Tracing.EventSource.IsSupported": false,
        "System.GC.Server": true,
        "System.Globalization.Invariant": false,
        "System.TimeZoneInfo.Invariant": false,
        "System.Linq.Enumerable.IsSizeOptimized": true,
        "System.Net.Http.EnableActivityPropagation": false,
        "System.Net.Http.WasmEnableStreamingResponse": true,
        "System.Net.SocketsHttpHandler.Http3Support": false,
        "System.Reflection.Metadata.MetadataUpdater.IsSupported": false,
        "System.Resources.ResourceManager.AllowCustomResourceTypes": false,
        "System.Resources.UseSystemResourceKeys": true,
        "System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported": true,
        "System.Runtime.InteropServices.BuiltInComInterop.IsSupported": false,
        "System.Runtime.InteropServices.EnableConsumingManagedCodeFromNativeHosting": false,
        "System.Runtime.InteropServices.EnableCppCLIHostActivation": false,
        "System.Runtime.InteropServices.Marshalling.EnableGeneratedComInterfaceComImportInterop": false,
        "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization": false,
        "System.StartupHookProvider.IsSupported": false,
        "System.Text.Encoding.EnableUnsafeUTF7Encoding": false,
        "System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault": true,
        "System.Threading.Thread.EnableAutoreleasePool": false,
        "Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException": false
      }
    }
  }
}/*json-end*/);export{gt as default,ft as dotnet,mt as exit};
