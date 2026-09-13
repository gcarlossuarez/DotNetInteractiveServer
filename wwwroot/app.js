'use strict';
const $ = id => document.getElementById(id);
const initial = 'using System;\n\nclass Program\n{\n    static void Main()\n    {\n        // Escribe aquí tu solución.\n        Console.WriteLine("Hola");\n    }\n}\n';
let activeProblem = '', busy = false, controller, records = [], complete = false, started = 0;
const key = id => 'judge-local-v1:' + (id || 'scratch');
function read(keyName, fallback) { try { return JSON.parse(localStorage.getItem(keyName)) ?? fallback; } catch { return fallback; } }
function persist() { try { localStorage.setItem(key(activeProblem), JSON.stringify({code:$('code').value,stdin:$('stdin').value,statement:$('statement').textContent})); $('saved').textContent='Borrador guardado'; } catch { $('saved').textContent='No se pudo guardar: descarga tu .cs'; } }
function restore(id) { const data=read(key(id),{}); $('code').value=data.code ?? initial; $('stdin').value=data.stdin ?? ''; $('statement').textContent=data.statement ?? ''; }
function base() { const url=new URL($('api').value); if(!['http:','https:'].includes(url.protocol)||!['localhost','127.0.0.1','[::1]'].includes(url.hostname)) throw Error('Usa una dirección local: localhost o 127.0.0.1.'); return url.origin; }
function status(text) { $('status').textContent=text; }
function setBusy(value) { busy=value; ['connect','problem','api','timeout','execute','validate','source','statementFile','testcase'].forEach(id=>$(id).disabled=value); $('cancel').disabled=!value; }
async function connect() { if(busy)return; $('connect').disabled=true; try { const response=await fetch(base()+'/datasets',{signal:AbortSignal.timeout(7000)}); if(!response.ok)throw Error('HTTP '+response.status); const data=await response.json(); if(!Array.isArray(data.problems))throw Error('Respuesta de catálogo no reconocida'); const wanted=activeProblem || read('judge-local-selected',''); $('problem').replaceChildren(new Option('Selecciona un problema','')); data.problems.forEach(p=>{const option=new Option(p.id+' · '+p.inputsCount+' casos',p.id); option.dataset.inputs=p.inputsCount; option.dataset.expected=p.expectedCount; $('problem').add(option);}); if(data.problems.some(p=>p.id===wanted))$('problem').value=wanted; else if(activeProblem)$('problem').add(new Option(activeProblem+' · no aparece instalado',activeProblem,true,true)); $('connection').textContent='API conectada'; updateDataset(); status('Catálogo actualizado. Selecciona un problema o ejecuta una entrada manual.'); } catch(e) { $('connection').textContent='Sin conexión'; status('No se pudo consultar la API. Verifica que DotNetInteractive esté ejecutándose. '+e.message); } finally { $('connect').disabled=false; } }
function updateDataset(){ loadCases();const o=$('problem').selectedOptions[0]; $('dataset').textContent=o?.dataset.inputs ? o.dataset.inputs+' entradas / '+o.dataset.expected+' salidas esperadas' : '';}
$('problem').addEventListener('change',()=>{persist();activeProblem=$('problem').value;restore(activeProblem);updateDataset();try{localStorage.setItem('judge-local-selected',JSON.stringify(activeProblem));}catch{} });
['code','stdin'].forEach(id=>$(id).addEventListener('input',persist));
$('code').addEventListener('keydown',e=>{if(e.key==='Tab'){e.preventDefault();const t=e.target;t.setRangeText('    ',t.selectionStart,t.selectionEnd,'end');persist();}});
function download(name,text,type='text/plain'){const url=URL.createObjectURL(new Blob([text],{type}));const a=document.createElement('a');a.href=url;a.download=name;a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);}
$('save').onclick=()=>download('Solucion_'+(activeProblem||'local')+'.cs',$('code').value);
$('report').onclick=()=>download('reporte_local_'+(activeProblem||'manual')+'.json',JSON.stringify({problem:activeProblem,official:false,complete,status:$('status').textContent,output:$('output').textContent,cases:records},null,2),'application/json');
async function openFile(id,target){const file=$(id).files[0];if(!file)return;if(file.size>2*1024*1024){status('El archivo supera 2 MB.');return;}if(target==='code' && $('code').value!==initial && !confirm('¿Reemplazar el código actual? Descarga tu fuente si deseas conservar otra copia.'))return;const text=await file.text();if(target==='code')$('code').value=text;else $('statement').textContent=text;persist();$(id).value='';}
$('source').onchange=()=>openFile('source','code');$('statementFile').onchange=()=>openFile('statementFile','statement');
function eventReceived(type,data){if(type==='start'){$('progress').max=data.totalCases||1;status('Validando '+data.totalCases+' casos…');}else if(type==='case-result'){records.push(data);$('progress').value=data.caseNumber;const item=document.createElement('details');item.className='case'+(data.result==='Accepted'?' ok':'');const title=document.createElement('summary');title.textContent=data.caseNumber+'. '+data.caseName+' — '+data.result+' · '+data.timeMs+' ms';const details=document.createElement('pre');details.textContent=[data.diff,data.validatorOutput].filter(Boolean).join('\n')||'Sin información adicional.';item.append(title,details);$('results').append(item);$('summary').textContent=records.filter(r=>r.result==='Accepted').length+' aceptados de '+records.length+' procesados';}else if(type==='complete'){complete=data.completed===true;}else if(type==='error'){throw Error(data.message||JSON.stringify(data));}}
async function stream(response){if(!response.body)throw Error('El navegador no permite leer la respuesta.');const reader=response.body.getReader(),decoder=new TextDecoder();let buffer='';function flush(final=false){let match;while((match=/\r?\n\r?\n/.exec(buffer))){const block=buffer.slice(0,match.index);buffer=buffer.slice(match.index+match[0].length);parse(block);}if(final&&buffer.trim())parse(buffer);}function parse(block){let type='message';const lines=[];for(const line of block.split(/\r?\n/)){if(line.startsWith('event:'))type=line.slice(6).trim();else if(line.startsWith('data:'))lines.push(line.slice(5).trimStart());}if(lines.length)eventReceived(type,JSON.parse(lines.join('\n')));}try{while(true){const {done,value}=await reader.read();if(done)break;buffer+=decoder.decode(value,{stream:true});flush();}buffer+=decoder.decode();flush(true);}finally{reader.releaseLock();}if(!complete)throw Error('La conexión terminó sin confirmar que finalizó la validación.');}
async function run(validation){if(busy)return;if(!$('code').value.trim()){status('Escribe o abre una solución C#.');return;}if(validation&&!activeProblem){status('Selecciona un problema instalado.');return;}const seconds=Number($('timeout').value);if(!Number.isFinite(seconds)||seconds<1||seconds>120){status('Elige un límite de 1 a 120 segundos.');return;}let endpoint;try{endpoint=base();}catch(e){status(e.message);return;}persist();setBusy(true);controller=new AbortController();records=[];complete=false;started=performance.now();$('results').replaceChildren();$('progress').value=0;$('output').textContent='';$('summary').textContent='Prueba local · sin envío oficial';status(validation?'Preparando el validador. Esta etapa puede tardar…':'Ejecutando entrada manual…');try{const response=await fetch(endpoint+(validation?'/validate-dataset':'/execute'),{method:'POST',headers:{'Content-Type':'application/json','Accept':validation?'text/event-stream':'application/json'},body:JSON.stringify({code:$('code').value,stdin:$('stdin').value,problem:activeProblem,timeoutMs:seconds*1000}),signal:controller.signal});if(!response.ok)throw Error('HTTP '+response.status+': '+(await response.text()).slice(0,3000));if(validation){if(!response.headers.get('content-type')?.includes('text/event-stream'))throw Error('La API no devolvió el progreso SSE esperado.');await stream(response);status('Validación terminada en '+((performance.now()-started)/1000).toFixed(1)+' s.');}else{const data=await response.json();$('output').textContent=typeof data.output==='string'?data.output:JSON.stringify(data,null,2);complete=true;status('Respuesta de la prueba manual recibida en '+((performance.now()-started)/1000).toFixed(1)+' s.');}}catch(e){status(e.name==='AbortError'?'Se interrumpió la espera. El motor podría seguir trabajando; comprueba su consola antes de ejecutar de nuevo.':'No se completó la prueba: '+e.message);}finally{setBusy(false);}}
$('connect').onclick=connect;$('execute').onclick=()=>run(false);$('validate').onclick=()=>run(true);$('cancel').onclick=()=>controller?.abort();

let caseGeneration = 0;
async function loadCases() {
    const generation=++caseGeneration, problem=activeProblem;
    $('execute').disabled=busy;
    $('testcase').replaceChildren(new Option(problem?'Cargando casos…':'Selecciona un problema primero',''));
    $('caseStatus').textContent='';
    if(!problem)return;
    try {
        const response=await fetch(base()+'/datasets/'+encodeURIComponent(problem),{signal:AbortSignal.timeout(7000)});
        if(!response.ok)throw Error('No se pudo listar los casos: HTTP '+response.status);
        const data=await response.json();
        if(generation!==caseGeneration)return;
        $('testcase').replaceChildren(new Option('Selecciona un dataset',''));
        for(const name of data.inputs ?? []) $('testcase').add(new Option(name,name));
        $('caseStatus').textContent=(data.inputs?.length ?? 0)+' entradas disponibles';
    }catch(e){if(generation===caseGeneration){$('testcase').replaceChildren(new Option('No se pudieron cargar los casos',''));$('caseStatus').textContent=e.message;}}
}
$('testcase').onchange=async()=>{
    const name=$('testcase').value, problem=activeProblem, generation=++caseGeneration;
    if(!name)return;
    $('caseStatus').textContent='Cargando '+name+'…';
    $('execute').disabled=true;
    try{
        const route='/datasets/'+encodeURIComponent(problem)+'/inputs/'+encodeURIComponent(name);
        let response=await fetch(base()+route,{signal:AbortSignal.timeout(7000)});
        // Compatibilidad con la API antigua que sigue activa: el host .NET lee Contests.
        if(response.status===404 && location.origin!==base()) response=await fetch(route,{signal:AbortSignal.timeout(7000)});
        if(!response.ok)throw Error((await response.text()).slice(0,300)||'No se pudo leer el caso.');
        const content=await response.text();
        if(generation!==caseGeneration||problem!==activeProblem)return;
        $('stdin').value=content;persist();
        $('caseStatus').textContent=name+' cargado · '+content.length+' caracteres';
    }catch(e){if(generation===caseGeneration)$('caseStatus').textContent='No se cargó el caso: '+e.message;}
    finally{if(generation===caseGeneration)$('execute').disabled=busy;}
};
activeProblem=read('judge-local-selected','');restore(activeProblem);connect();


