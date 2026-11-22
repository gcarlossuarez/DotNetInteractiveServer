'use strict';

(() => {
    const editor = document.getElementById('code');
    const toggle = document.getElementById('clipboardToggle');
    const notice = document.getElementById('clipboardStatus');
    const dialog = document.getElementById('teacherDialog');
    const password = document.getElementById('teacherPassword');
    const message = document.getElementById('teacherMessage');
    const submit = document.getElementById('teacherSubmit');

    let enabled = false;

    // Función que bloquea la acción si el portapapeles no está habilitado
    function block(e) {
        if (!enabled) {
            e.preventDefault();
            notice.textContent = 'Operación bloqueada. El docente puede habilitar el portapapeles con su clave.';
        }
    }

    // Registrar los eventos restrictivos
    for (const type of ['copy', 'cut', 'paste', 'drop', 'dragstart', 'contextmenu']) {
        editor.addEventListener(type, block);
    }

    editor.addEventListener('beforeinput', e => {
        const blockedTypes = [
            'insertFromPaste',
            'insertFromDrop',
            'insertFromPasteAsQuotation',
            'deleteByCut',
            'deleteByDrag'
        ];
        if (blockedTypes.includes(e.inputType)) {
            block(e);
        }
    });

    editor.addEventListener('keydown', e => {
        const key = e.key.toLowerCase();
        const ctrlCombo = (e.ctrlKey || e.metaKey) && ['c', 'v', 'x', 'insert'].includes(key);
        const shiftCombo = e.shiftKey && ['insert', 'delete'].includes(key);
        if (ctrlCombo || shiftCombo) {
            block(e);
        }
    });

    // Actualizar textos según el estado
    function update() {
        toggle.textContent = 'Portapapeles: ' + (enabled ? 'habilitado · bloquear' : 'bloqueado');
        notice.textContent = enabled
            ? 'El docente habilitó copiar, cortar y pegar hasta que se recargue o se bloquee esta página.'
            : 'El editor no permite copiar, cortar, pegar ni arrastrar texto.';
    }

    // Manejo del botón para abrir el diálogo o bloquear manualmente
    toggle.onclick = () => {
        if (enabled) {
            enabled = false;
            update();
            return;
        }

        message.textContent = '';
        password.value = '';
        dialog.showModal();
        password.focus();
    };

    // Botón cancelar del diálogo
    document.getElementById('teacherCancel').onclick = () => {
        dialog.close();
    };

    dialog.addEventListener('close', () => {
        password.value = '';
    });

    // Envío del formulario de la clave docente
    document.getElementById('teacherForm').onsubmit = async e => {
        e.preventDefault();
        submit.disabled = true;
        message.textContent = 'Comprobando…';

        try {
            const response = await fetch('/teacher/editor-unlock', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ password: password.value }),
                signal: AbortSignal.timeout(10000)
            });

            password.value = '';
            const data = await response.json();

            if (!response.ok || data.enabled !== true) {
                throw Error(data.message || 'No se pudo habilitar.');
            }

            if (!dialog.open) return;

            enabled = true;
            update();
            dialog.close();
            editor.focus();

        } catch (error) {
            message.textContent = error.message;
        } finally {
            submit.disabled = false;
        }
    };
})();