// ============================================
// SAFE FLOATING WINDOWS MANAGER
// Non-blocking, page-safe floating windows
// ============================================

class SafeFloatingWindow {
    constructor() {
        this.windows = new Map();
        this.init();
    }

    init() {
        console.log('🚀 SafeFloatingWindow initialized');

        // Always start with unlocked page
        this.unlockPage();

        // Setup escape key listener
        this.setupEscapeListener();

        // Add global close button for emergencies
        this.addEmergencyButton();
    }

    unlockPage() {
        // Force unlock the entire page
        Object.assign(document.body.style, {
            overflow: 'auto',
            position: 'static',
            pointerEvents: 'auto',
            height: 'auto'
        });

        // Remove any modal classes
        document.body.classList.remove('modal-open');

        // Remove any blocking backdrops
        document.querySelectorAll('.modal-backdrop, .overlay, .backdrop').forEach(el => {
            el.remove();
        });

        console.log('✅ Page unlocked');
    }

    setupEscapeListener() {
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closeAll();
            }
        });
    }

    //addEmergencyButton() {
    //    // Remove existing button if any
    //    const oldBtn = document.getElementById('emergency-close-all');
    //    if (oldBtn) oldBtn.remove();

    //    // Create emergency button
    //    const btn = document.createElement('button');
    //    btn.id = 'emergency-close-all';
    //    btn.innerHTML = '🆘 CLOSE ALL';
    //    btn.title = 'Close all floating windows';

    //    Object.assign(btn.style, {
    //        position: 'fixed',
    //        bottom: '20px',
    //        left: '20px',
    //        zIndex: '99999',
    //        background: '#ef4444',
    //        color: 'white',
    //        border: 'none',
    //        padding: '8px 12px',
    //        borderRadius: '6px',
    //        cursor: 'pointer',
    //        fontSize: '12px',
    //        fontWeight: 'bold',
    //        boxShadow: '0 4px 12px rgba(0,0,0,0.3)'
    //    });

    //    btn.onclick = () => {
    //        this.closeAll();
    //        this.unlockPage();
    //    };

    //    document.body.appendChild(btn);
    //}

    create(options = {}) {
        const id = options.id || 'window-' + Date.now();
        const config = {
            id,
            title: options.title || 'Window',
            content: options.content || '',
            width: options.width || '500px',
            height: options.height || 'auto',
            x: options.x || 100,
            y: options.y || 100,
            closable: options.closable !== false,
            draggable: options.draggable !== false,
            resizable: options.resizable || false,
            onClose: options.onClose || null
        };

        // Ensure page is unlocked
        this.unlockPage();

        // Remove existing window with same ID
        this.close(id);

        // Create window element
        const windowEl = document.createElement('div');
        windowEl.id = `safe-window-${id}`;
        windowEl.className = 'safe-floating-window';
        windowEl.dataset.windowId = id;

        // Apply styles - WINDOW DOES NOT BLOCK PAGE
        Object.assign(windowEl.style, {
            position: 'fixed',
            top: `${config.y}px`,
            left: `${config.x}px`,
            width: config.width,
            maxHeight: config.height === 'auto' ? '80vh' : config.height,
            minHeight: '150px',
            background: 'var(--card-bg)',
            border: '2px solid var(--accent-primary)',
            borderRadius: '12px',
            boxShadow: '0 15px 50px rgba(0,0,0,0.25)',
            zIndex: '1060',
            display: 'flex',
            flexDirection: 'column',
            overflow: 'hidden',
            pointerEvents: 'auto',
            resize: config.resizable ? 'both' : 'none'
        });

        // Create header
        const header = document.createElement('div');
        header.className = 'safe-window-header';
        Object.assign(header.style, {
            background: 'linear-gradient(135deg, var(--accent-primary), var(--accent-secondary))',
            padding: '12px 16px',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            color: 'white',
            fontWeight: 'bold',
            cursor: config.draggable ? 'move' : 'default',
            userSelect: 'none'
        });

        const titleSpan = document.createElement('span');
        titleSpan.textContent = config.title;
        titleSpan.style.flex = '1';
        header.appendChild(titleSpan);

        // Close button
        if (config.closable) {
            const closeBtn = document.createElement('button');
            closeBtn.innerHTML = '×';
            closeBtn.title = 'Close window';
            Object.assign(closeBtn.style, {
                background: 'rgba(255,255,255,0.3)',
                border: 'none',
                color: 'white',
                width: '28px',
                height: '28px',
                borderRadius: '50%',
                cursor: 'pointer',
                fontSize: '20px',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                marginLeft: '10px',
                transition: 'background 0.2s'
            });

            closeBtn.onmouseenter = () => {
                closeBtn.style.background = 'rgba(255,255,255,0.5)';
            };

            closeBtn.onmouseleave = () => {
                closeBtn.style.background = 'rgba(255,255,255,0.3)';
            };

            closeBtn.onclick = (e) => {
                e.stopPropagation();
                this.close(id);
                if (config.onClose) config.onClose();
            };

            header.appendChild(closeBtn);
        }

        // Create content area
        const contentDiv = document.createElement('div');
        contentDiv.className = 'safe-window-content';
        Object.assign(contentDiv.style, {
            padding: '20px',
            overflow: 'auto',
            flex: '1',
            background: 'var(--card-bg)',
            color: 'var(--text-primary)'
        });

        // Set content (can be HTML string or DOM element)
        if (typeof config.content === 'string') {
            contentDiv.innerHTML = config.content;
        } else if (config.content instanceof HTMLElement) {
            contentDiv.appendChild(config.content);
        } else {
            contentDiv.textContent = String(config.content);
        }

        // Assemble window
        windowEl.appendChild(header);
        windowEl.appendChild(contentDiv);

        // Add to body (directly, no blocking container)
        document.body.appendChild(windowEl);

        // Make draggable if enabled
        if (config.draggable) {
            this.makeDraggable(windowEl, header);
        }

        // Store reference
        this.windows.set(id, {
            element: windowEl,
            config: config
        });

        // Bring to front on click
        windowEl.addEventListener('mousedown', () => {
            this.bringToFront(id);
        });

        // Ensure page stays unlocked
        this.unlockPage();

        console.log(`✅ Window created: ${id}`);
        return windowEl;
    }

    makeDraggable(element, handle) {
        let isDragging = false;
        let offsetX, offsetY;

        handle.addEventListener('mousedown', (e) => {
            isDragging = true;
            offsetX = e.clientX - element.offsetLeft;
            offsetY = e.clientY - element.offsetTop;
            element.style.cursor = 'grabbing';

            const onMouseMove = (e) => {
                if (!isDragging) return;
                element.style.left = `${e.clientX - offsetX}px`;
                element.style.top = `${e.clientY - offsetY}px`;
            };

            const onMouseUp = () => {
                isDragging = false;
                element.style.cursor = '';
                document.removeEventListener('mousemove', onMouseMove);
                document.removeEventListener('mouseup', onMouseUp);
            };

            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);

            e.preventDefault();
        });

        handle.addEventListener('mouseup', () => {
            isDragging = false;
            element.style.cursor = '';
        });
    }

    bringToFront(id) {
        const windowData = this.windows.get(id);
        if (!windowData) return;

        // Get current max z-index
        let maxZ = 1060;
        this.windows.forEach((data, winId) => {
            const z = parseInt(data.element.style.zIndex) || 1060;
            if (z > maxZ) maxZ = z;
        });

        // Set this window to front
        windowData.element.style.zIndex = maxZ + 1;
    }

    show(id) {
        const windowData = this.windows.get(id);
        if (windowData) {
            windowData.element.style.display = 'flex';
            this.bringToFront(id);
            this.unlockPage();
            return true;
        }
        return false;
    }

    close(id) {
        const windowData = this.windows.get(id);
        if (windowData) {
            if (windowData.element.parentNode) {
                windowData.element.remove();
            }
            this.windows.delete(id);
            this.unlockPage();

            if (windowData.config.onClose) {
                windowData.config.onClose();
            }

            console.log(`✅ Window closed: ${id}`);
            return true;
        }
        return false;
    }

    closeAll() {
        const closed = [];
        this.windows.forEach((data, id) => {
            if (this.close(id)) {
                closed.push(id);
            }
        });
        this.unlockPage();
        console.log(`✅ Closed all windows: ${closed.length} windows`);
        return closed;
    }

    // Quick modal helper
    modal(title, content, options = {}) {
        const id = 'modal-' + Date.now();
        return this.create({
            id,
            title,
            content,
            width: options.width || '500px',
            height: options.height || 'auto',
            x: options.x || Math.max(50, window.innerWidth / 2 - 250),
            y: options.y || 80,
            closable: true,
            draggable: true,
            ...options
        });
    }

    // Alert helper
    alert(message, title = 'Alert') {
        return this.modal(title, `
            <div style="padding: 20px;">
                <p style="margin-bottom: 20px;">${message}</p>
                <div style="text-align: right;">
                    <button onclick="window.SafeWindows.close('${'alert-' + Date.now()}')" 
                            style="background: var(--accent-primary); color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer;">
                        OK
                    </button>
                </div>
            </div>
        `, { width: '400px' });
    }

    // Confirm helper
    confirm(message, title = 'Confirm', onConfirm, onCancel) {
        const id = 'confirm-' + Date.now();
        return this.modal(title, `
            <div style="padding: 20px;">
                <p style="margin-bottom: 20px;">${message}</p>
                <div style="display: flex; justify-content: flex-end; gap: 10px;">
                    <button onclick="
                        if (window.SafeWindows) window.SafeWindows.close('${id}');
                        ${onCancel ? `(${onCancel.toString()})();` : ''}
                    " style="background: var(--accent-secondary); color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer;">
                        Cancel
                    </button>
                    <button onclick="
                        if (window.SafeWindows) window.SafeWindows.close('${id}');
                        ${onConfirm ? `(${onConfirm.toString()})();` : ''}
                    " style="background: var(--accent-primary); color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer;">
                        Confirm
                    </button>
                </div>
            </div>
        `, { width: '400px' });
    }
}

// ============================================
// GLOBAL INITIALIZATION
// ============================================

// Create global instance
let SafeWindows = null;

// Initialize when DOM is ready
//document.addEventListener('DOMContentLoaded', () => {
//    SafeWindows = new SafeFloatingWindow();
//    window.SafeWindows = SafeWindows;

//    // Add test button for debugging
//    if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
//        const testBtn = document.createElement('button');
//        testBtn.id = 'safe-window-test-btn';
//        testBtn.innerHTML = '🧪 Test Window';
//        testBtn.title = 'Test safe floating window';

//        Object.assign(testBtn.style, {
//            position: 'fixed',
//            bottom: '60px',
//            right: '20px',
//            zIndex: '99999',
//            background: '#10b981',
//            color: 'white',
//            border: 'none',
//            padding: '10px 15px',
//            borderRadius: '8px',
//            cursor: 'pointer',
//            fontWeight: 'bold',
//            boxShadow: '0 4px 12px rgba(0,0,0,0.2)'
//        });

//        testBtn.onclick = () => {
//            window.SafeWindows.modal(
//                '🧪 Safe Window Test',
//                `
//                <div style="padding: 20px;">
//                    <h3 style="color: var(--accent-primary); margin-top: 0;">✅ SAFE WINDOW TEST</h3>
//                    <p>This window <strong>DOES NOT</strong> block the page!</p>
                    
//                    <div style="background: var(--secondary-bg); padding: 15px; border-radius: 8px; margin: 15px 0;">
//                        <h4>Test Instructions:</h4>
//                        <ol>
//                            <li>Try to scroll the page <strong>behind</strong> this window</li>
//                            <li>Try to click other page elements</li>
//                            <li>Drag this window by the header</li>
//                            <li>Close with X button or Escape key</li>
//                            <li>Click the "Test Interaction" button below</li>
//                        </ol>
//                    </div>
                    
//                    <div style="text-align: center; margin: 20px 0;">
//                        <button onclick="alert('Button inside window works!')" 
//                                style="background: var(--accent-primary); color: white; border: none; padding: 10px 20px; border-radius: 6px; cursor: pointer;">
//                            Test Interaction
//                        </button>
//                    </div>
                    
//                    <div style="background: rgba(16, 185, 129, 0.1); padding: 15px; border-radius: 8px; border-left: 4px solid var(--accent-success);">
//                        <strong>✅ SUCCESS CRITERIA:</strong>
//                        <p style="margin: 5px 0 0 0;">If you can interact with the page behind this window, everything is working perfectly!</p>
//                    </div>
//                </div>
//                `,
//                { width: '550px' }
//            );
//        };

//        document.body.appendChild(testBtn);
//    }

//    console.log('✅ SafeFloatingWindow system ready');
//});

// Global shortcut functions
window.showWindow = function (title, content, options) {
    if (!window.SafeWindows) {
        window.SafeWindows = new SafeFloatingWindow();
    }
    return window.SafeWindows.modal(title, content, options);
};

window.closeAllWindows = function () {
    if (window.SafeWindows) {
        window.SafeWindows.closeAll();
    }
};