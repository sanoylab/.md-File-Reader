// Markdown preview and document management
let currentDocumentId = null;
let debounceTimer = null;
let documentCount = 0;
let documentLimit = 500;

// Handle authentication errors - redirect to login if unauthorized
function handleAuthError(response) {
    if (response.status === 401) {
        window.location.href = '/Account/Login';
        return true;
    }
    return false;
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    initializeMarkdownPreview();
    initializeFileUpload();
    loadDocuments();
    initializeResizer();
    initializeSidebar();
});

// Toggle sidebar visibility
function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    const openBtn = document.getElementById('sidebar-open-btn');
    const mainContent = document.querySelector('.main-content-area');
    
    if (sidebar.classList.contains('hidden')) {
        sidebar.classList.remove('hidden');
        openBtn.style.display = 'none';
        if (mainContent) {
            mainContent.style.marginLeft = '280px';
        }
        // Save preference
        localStorage.setItem('sidebarVisible', 'true');
    } else {
        sidebar.classList.add('hidden');
        openBtn.style.display = 'flex';
        if (mainContent) {
            mainContent.style.marginLeft = '0';
        }
        // Save preference
        localStorage.setItem('sidebarVisible', 'false');
    }
}

// Initialize sidebar state
function initializeSidebar() {
    const sidebarVisible = localStorage.getItem('sidebarVisible');
    const sidebar = document.getElementById('sidebar');
    const openBtn = document.getElementById('sidebar-open-btn');
    const mainContent = document.querySelector('.main-content-area');
    
    // Default to visible if no preference saved
    if (sidebarVisible === 'false') {
        sidebar.classList.add('hidden');
        openBtn.style.display = 'flex';
        if (mainContent) {
            mainContent.style.marginLeft = '0';
        }
    } else {
        // Sidebar visible by default
        sidebar.classList.remove('hidden');
        openBtn.style.display = 'none';
        if (mainContent) {
            mainContent.style.marginLeft = '280px';
        }
    }
}

// Initialize markdown preview with debouncing
function initializeMarkdownPreview() {
    const input = document.getElementById('markdown-input');
    const preview = document.getElementById('markdown-preview');

    input.addEventListener('input', function() {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            updatePreview();
        }, 300);
    });
}

// Update markdown preview
function updatePreview() {
    const input = document.getElementById('markdown-input');
    const preview = document.getElementById('markdown-preview');
    const markdown = input.value;

    if (!markdown.trim()) {
        preview.innerHTML = `
            <div class="empty-state">
                <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" opacity="0.3">
                    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
                    <polyline points="14 2 14 8 20 8"></polyline>
                    <line x1="16" y1="13" x2="8" y2="13"></line>
                    <line x1="16" y1="17" x2="8" y2="17"></line>
                    <polyline points="10 9 9 9 8 9"></polyline>
                </svg>
                <p style="margin-top: 1rem; font-size: 0.9rem;">Start writing to see the preview</p>
            </div>
        `;
        return;
    }

    if (typeof marked !== 'undefined') {
        preview.style.opacity = '0';
        setTimeout(() => {
            preview.innerHTML = marked.parse(markdown);
            preview.style.opacity = '1';
        }, 100);
    } else {
        preview.textContent = markdown;
    }
}

// Initialize file upload
function initializeFileUpload() {
    const fileInput = document.getElementById('file-input');
    fileInput.addEventListener('change', function(e) {
        const file = e.target.files[0];
        if (file && file.name.endsWith('.md')) {
            const reader = new FileReader();
            reader.onload = function(e) {
                document.getElementById('markdown-input').value = e.target.result;
                currentDocumentId = null; // Clear current document ID so it creates a new document
                updatePreview();
            };
            reader.readAsText(file);
        } else {
            alert('Please select a .md file');
        }
    });
}

// Load documents list
async function loadDocuments() {
    try {
        const response = await fetch('/api/Document');
        if (!response.ok) {
            if (handleAuthError(response)) return;
            throw new Error('Failed to load documents');
        }
        
        const data = await response.json();
        const documents = data.documents || data; // Handle both old and new response format
        documentCount = data.documentCount || documents.length;
        documentLimit = data.documentLimit || 500;
        
        displayDocuments(documents);
        updateDocumentCountDisplay();
    } catch (error) {
        console.error('Error loading documents:', error);
        document.getElementById('documents-list').innerHTML = 
            '<div class="text-danger">Failed to load documents</div>';
    }
}

// Update document count display in sidebar
function updateDocumentCountDisplay() {
    const countElement = document.getElementById('document-count');
    if (countElement) {
        if (documentLimit === -1) {
            countElement.textContent = `${documentCount} documents`;
            countElement.className = 'document-count unlimited';
        } else {
            countElement.textContent = `${documentCount} / ${documentLimit}`;
            countElement.className = 'document-count';
            
            // Show warning if approaching limit (90%)
            const warningThreshold = Math.floor(documentLimit * 0.9);
            if (documentCount >= warningThreshold) {
                countElement.classList.add('warning');
            }
        }
    }
}

// Display documents in sidebar
function displayDocuments(documents) {
    const list = document.getElementById('documents-list');
    
    if (documents.length === 0) {
        list.innerHTML = `
            <div class="empty-state">
                <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" opacity="0.3">
                    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
                    <polyline points="14 2 14 8 20 8"></polyline>
                </svg>
                <p style="margin-top: 0.5rem; font-size: 0.85rem;">No documents yet</p>
                <p style="font-size: 0.75rem; opacity: 0.7;">Create your first document!</p>
            </div>
        `;
        return;
    }

    list.innerHTML = documents.map((doc, index) => `
        <div class="document-item" data-id="${doc.id}" onclick="loadDocument('${doc.id}')" style="animation-delay: ${index * 0.05}s;">
            <h6 class="card-title mb-1">${escapeHtml(doc.title)}</h6>
            <small>${formatDate(doc.updatedAt)}</small>
            <div class="mt-2">
                <button class="btn btn-sm btn-danger" onclick="event.stopPropagation(); deleteDocument('${doc.id}')">
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle;">
                        <polyline points="3 6 5 6 21 6"></polyline>
                        <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                    </svg>
                </button>
            </div>
        </div>
    `).join('');
}

// Load a document
async function loadDocument(id) {
    try {
        const response = await fetch(`/api/Document/${id}`);
        if (!response.ok) {
            if (handleAuthError(response)) return;
            throw new Error('Failed to load document');
        }
        
        const docData = await response.json();
        document.getElementById('markdown-input').value = docData.content;
        currentDocumentId = docData.id;
        updatePreview();
        
        // Highlight selected document
        document.querySelectorAll('.document-item').forEach(item => {
            item.classList.remove('border-primary');
        });
        const selectedItem = document.querySelector(`[data-id="${id}"]`);
        if (selectedItem) {
            selectedItem.classList.add('border-primary');
            selectedItem.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }
    } catch (error) {
        console.error('Error loading document:', error);
        alert('Failed to load document');
    }
}

// Save document
async function saveDocument() {
    const content = document.getElementById('markdown-input').value;
    if (!content.trim()) {
        showToast('Please enter some content', 'danger');
        return;
    }

    // Check limit before saving (only for new documents)
    if (!currentDocumentId && documentLimit !== -1 && documentCount >= documentLimit) {
        showToast(`Document limit reached. You have reached the maximum of ${documentLimit} documents. Please delete some documents to create new ones.`, 'danger');
        return;
    }

    const saveBtn = document.getElementById('save-btn');
    const originalText = saveBtn.innerHTML;
    saveBtn.disabled = true;
    saveBtn.innerHTML = '<span class="loading" style="margin-right: 6px;"></span>Saving...';
    saveBtn.classList.add('loading');

    try {
        const requestBody = {
            content: content
        };
        
        // Only include id if we're updating an existing document
        if (currentDocumentId) {
            requestBody.id = currentDocumentId;
        }

        const response = await fetch('/api/Document', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestBody)
        });

        if (!response.ok) {
            if (handleAuthError(response)) return;
            
            const errorData = await response.json().catch(() => ({ error: 'Failed to save document' }));
            throw new Error(errorData.error || 'Failed to save document');
        }

        const result = await response.json();
        currentDocumentId = result.id;
        
        // Update document count from response
        if (result.documentCount !== undefined) {
            documentCount = result.documentCount;
            documentLimit = result.documentLimit || documentLimit;
            updateDocumentCountDisplay();
        }
        
        // Show success message
        showToast('Document saved successfully!', 'success');
        
        // Reload documents list
        loadDocuments();
    } catch (error) {
        console.error('Error saving document:', error);
        showToast(error.message || 'Failed to save document', 'danger');
    } finally {
        saveBtn.disabled = false;
        saveBtn.innerHTML = originalText;
        saveBtn.classList.remove('loading');
    }
}

// Create new document
function newDocument() {
    document.getElementById('markdown-input').value = '';
    currentDocumentId = null;
    updatePreview();
    
    // Remove highlight from all documents
    document.querySelectorAll('.document-item').forEach(item => {
        item.classList.remove('border-primary');
    });
    
    showToast('New document created', 'success');
}

// Delete document
async function deleteDocument(id) {
    if (!confirm('Are you sure you want to delete this document?')) {
        return;
    }

    try {
        const response = await fetch(`/api/Document/${id}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            if (handleAuthError(response)) return;
            throw new Error('Failed to delete document');
        }

        if (currentDocumentId === id) {
            document.getElementById('markdown-input').value = '';
            currentDocumentId = null;
            updatePreview();
        }

        // Decrement document count
        if (documentCount > 0) {
            documentCount--;
            updateDocumentCountDisplay();
        }

        loadDocuments();
        showToast('Document deleted successfully!', 'success');
    } catch (error) {
        console.error('Error deleting document:', error);
        alert('Failed to delete document');
    }
}

// Export document
async function exportDocument(format) {
    const content = document.getElementById('markdown-input').value;
    if (!content.trim()) {
        alert('Please enter some content to export');
        return;
    }

    try {
        const response = await fetch(`/api/Export/${format}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                documentId: currentDocumentId,
                content: content
            })
        });

        if (!response.ok) {
            if (handleAuthError(response)) return;
            throw new Error('Failed to export document');
        }

        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `document.${format === 'pdf' ? 'pdf' : 'docx'}`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
    } catch (error) {
        console.error('Error exporting document:', error);
        alert('Failed to export document');
    }
}

// Initialize resizable panels
function initializeResizer() {
    const inputPanel = document.getElementById('input-panel');
    const previewPanel = document.getElementById('preview-panel');
    const resizer = document.getElementById('middle-resizer');

    if (!resizer || !inputPanel || !previewPanel) return;

    let isResizing = false;

    resizer.addEventListener('mousedown', function(e) {
        isResizing = true;
        document.body.style.cursor = 'col-resize';
        document.body.style.userSelect = 'none';
        e.preventDefault();
    });

    document.addEventListener('mousemove', function(e) {
        if (!isResizing) return;

        const container = inputPanel.parentElement;
        const containerRect = container.getBoundingClientRect();
        const mouseX = e.clientX - containerRect.left;
        const containerWidth = containerRect.width;
        const newWidthPercent = (mouseX / containerWidth) * 100;

        // Limit resizing between 20% and 80%
        if (newWidthPercent > 20 && newWidthPercent < 80) {
            inputPanel.style.width = newWidthPercent + '%';
            previewPanel.style.width = (100 - newWidthPercent) + '%';
        }
    });

    document.addEventListener('mouseup', function() {
        if (isResizing) {
            isResizing = false;
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
        }
    });
}

// Utility functions
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function showToast(message, type) {
    // Modern toast notification
    const toast = document.createElement('div');
    const bgColor = type === 'success' ? 'var(--accent-secondary)' : 'var(--accent-danger)';
    toast.className = 'position-fixed';
    toast.style.cssText = `
        top: 80px;
        right: 20px;
        z-index: 9999;
        min-width: 280px;
        background: ${bgColor};
        color: white;
        padding: 1rem 1.25rem;
        border-radius: 8px;
        box-shadow: 0 8px 24px rgba(0, 0, 0, 0.3);
        animation: slideInRight 0.3s ease-out;
        font-weight: 500;
    `;
    toast.innerHTML = `
        <div class="d-flex align-items-center justify-content-between">
            <span>${message}</span>
            <button type="button" class="btn-close btn-close-white ms-3" onclick="this.parentElement.parentElement.remove()" style="opacity: 0.8;"></button>
        </div>
    `;
    document.body.appendChild(toast);
    
    setTimeout(() => {
        toast.style.animation = 'slideOutRight 0.3s ease-out';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// Add toast animations to CSS
if (!document.getElementById('toast-styles')) {
    const style = document.createElement('style');
    style.id = 'toast-styles';
    style.textContent = `
        @keyframes slideInRight {
            from {
                transform: translateX(100%);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
        @keyframes slideOutRight {
            from {
                transform: translateX(0);
                opacity: 1;
            }
            to {
                transform: translateX(100%);
                opacity: 0;
            }
        }
    `;
    document.head.appendChild(style);
}

