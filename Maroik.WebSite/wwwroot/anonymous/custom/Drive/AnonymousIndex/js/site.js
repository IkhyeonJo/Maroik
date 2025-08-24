$(document).ready(function () {
    let selectedItems = [];
    let currentPath = '/';
    let viewMode = 'grid';
    let clipboard = { items: [], action: null };

    // File Manager Class
    class FileManager {
        constructor() {
            this.initializeEvents();
            this.loadDirectory(currentPath);
        }

        initializeEvents() {
            // Toolbar buttons
            $('#btn-upload').on('click', () => this.showUploadZone());
            $('#btn-new-folder').on('click', () => this.showNewFolderModal());
            $('#btn-rename').on('click', () => this.renameSelected());
            $('#btn-copy').on('click', () => this.copySelected());
            $('#btn-move').on('click', () => this.moveSelected());
            $('#btn-delete').on('click', () => this.deleteSelected());
            $('#btn-download').on('click', () => this.downloadSelected());
            $('#btn-details').on('click', () => this.showFileDetails());
            $('#btn-open-folder').on('click', () => this.openSelectedItems());
            $('#btn-copy-folder').on('click', () => this.copySelected());
            $('#btn-paste').on('click', () => this.pasteItems());
            $('#btn-clear-clipboard').on('click', () => this.clearClipboard());

            // File item events
            $(document).on('click', '.file-item', (e) => this.selectItem(e));
            $(document).on('dblclick', '.file-item', (e) => this.openItem(e));
            $(document).on('contextmenu', '.file-item', (e) => this.showContextMenu(e));

            // Breadcrumb navigation
            $(document).on('click', '.breadcrumb-path', (e) => this.navigateTo($(e.target).data('path')));

            // Search functionality
            $('#search-input').on('input', (e) => this.searchFiles(e.target.value));

            // Upload zone events
            this.initializeUploadZone();

            // Context menu events
            $(document).on('click', '.context-menu-item', (e) => this.executeContextAction(e));
            $(document).on('click', () => this.hideContextMenu());

            // Modal events
            $('#create-folder-btn').on('click', () => this.createFolder());
            $('#rename-confirm-btn').on('click', () => this.confirmRename());
            $('#btn-download-from-details').on('click', () => this.downloadFromDetails());

            // Keyboard shortcuts
            $(document).on('keydown', (e) => this.handleKeyboard(e));
        }

        setViewMode(mode) {
            viewMode = 'grid'; // Always use grid mode
            const $content = $('#file-content');
            $content.removeClass('file-list').addClass('file-grid');
        }

        selectItem(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const $item = $(e.currentTarget);
            const itemPath = $item.data('path');
            const isAlreadySelected = $item.hasClass('selected');

            // If Ctrl/Cmd not held, clear other selections unless clicking already selected item
            if (!e.ctrlKey && !e.metaKey && !isAlreadySelected) {
                $('.file-item').removeClass('selected');
                selectedItems = [];
            }

            // Toggle selection of the clicked item
            if ($item.hasClass('selected')) {
                $item.removeClass('selected');
                selectedItems = selectedItems.filter(path => path !== itemPath);
            } else {
                $item.addClass('selected');
                selectedItems.push(itemPath);
            }

            this.updateToolbarButtons();
        }

        openItem(e) {
            const $item = $(e.currentTarget);
            const itemType = $item.data('type');
            const itemPath = $item.data('path');

            if (itemType === 'folder') {
                this.navigateTo(itemPath);
            } else {
                this.downloadFile(itemPath);
            }
        }

        showContextMenu(e) {
            e.preventDefault();
            const $contextMenu = $('#context-menu');
            const $item = $(e.currentTarget);
            
            if (!$item.hasClass('selected')) {
                $('.file-item').removeClass('selected');
                $item.addClass('selected');
                selectedItems = [$item.data('path')];
            }

            $contextMenu.css({
                display: 'block',
                left: e.pageX,
                top: e.pageY
            });

            this.updateToolbarButtons();
        }

        hideContextMenu() {
            $('#context-menu').hide();
        }

        executeContextAction(e) {
            const action = $(e.currentTarget).data('action');
            this.hideContextMenu();

            switch (action) {
                case 'open':
                    this.openSelectedItems();
                    break;
                case 'rename':
                    this.renameSelected();
                    break;
                case 'copy':
                    this.copySelected();
                    break;
                case 'move':
                    this.moveSelected();
                    break;
                case 'download':
                    this.downloadSelected();
                    break;
                case 'details':
                    this.showFileDetails();
                    break;
                case 'delete':
                    this.deleteSelected();
                    break;
            }
        }

        navigateTo(path) {
            currentPath = path;
            this.loadDirectory(path);
            this.updateBreadcrumb(path);
        }

        updateBreadcrumb(path) {
            const $breadcrumb = $('#breadcrumb');
            const pathParts = path.split('/').filter(part => part);
            
            $breadcrumb.empty();
            
            // Home breadcrumb
            $breadcrumb.append(`
                <li class="breadcrumb-item">
                    <i class="fas fa-home"></i>
                    <span class="breadcrumb-path" data-path="/" style="cursor: pointer;">Home</span>
                </li>
            `);

            // Path breadcrumbs
            let currentPathBuilder = '';
            pathParts.forEach((part, index) => {
                currentPathBuilder += '/' + part;
                const isLast = index === pathParts.length - 1;
                
                $breadcrumb.append(`
                    <li class="breadcrumb-item ${isLast ? 'active' : ''}">
                        <span class="breadcrumb-path" data-path="${currentPathBuilder}" 
                              style="${isLast ? '' : 'cursor: pointer;'}">${part}</span>
                    </li>
                `);
            });
        }

        loadDirectory(path) {
            // Simulate loading directory contents
            // In a real implementation, this would make an AJAX call to the server
            const $content = $('#file-content');
            $content.empty();

            // Mock data based on path
            const mockData = this.getMockDirectoryData(path);
            
            mockData.forEach(item => {
                const iconClass = this.getFileIcon(item.name, item.type);
                const $item = $(`
                    <div class="file-item ${item.type === 'folder' ? 'folder-item' : 'file-type'}" 
                         data-name="${item.name}" data-type="${item.type}" data-path="${item.path}">
                        <div class="text-center">
                            <i class="${iconClass} file-icon"></i>
                            <div class="file-name">${item.name}</div>
                        </div>
                    </div>
                `);
                $content.append($item);
            });

            selectedItems = [];
            this.updateToolbarButtons();
        }

        getMockDirectoryData(path) {
            // Mock directory structure for anonymous users
            const directories = {
                '/': [
                    { name: 'Public Documents', type: 'folder', path: '/Public Documents' },
                    { name: 'Shared Images', type: 'folder', path: '/Shared Images' },
                    { name: 'Public Downloads', type: 'folder', path: '/Public Downloads' },
                    { name: 'readme.txt', type: 'file', path: '/readme.txt' },
                    { name: 'sample.pdf', type: 'file', path: '/sample.pdf' },
                    { name: 'demo.jpg', type: 'file', path: '/demo.jpg' }
                ],
                '/Public Documents': [
                    { name: 'guide.docx', type: 'file', path: '/Public Documents/guide.docx' },
                    { name: 'manual.pdf', type: 'file', path: '/Public Documents/manual.pdf' },
                    { name: 'Templates', type: 'folder', path: '/Public Documents/Templates' }
                ],
                '/Shared Images': [
                    { name: 'banner.jpg', type: 'file', path: '/Shared Images/banner.jpg' },
                    { name: 'logo.png', type: 'file', path: '/Shared Images/logo.png' },
                    { name: 'Gallery', type: 'folder', path: '/Shared Images/Gallery' }
                ],
                '/Public Downloads': [
                    { name: 'software.zip', type: 'file', path: '/Public Downloads/software.zip' },
                    { name: 'resources.rar', type: 'file', path: '/Public Downloads/resources.rar' }
                ]
            };

            return directories[path] || [];
        }

        getFileIcon(filename, type) {
            if (type === 'folder') {
                return 'fas fa-folder folder-icon';
            }

            const extension = filename.split('.').pop().toLowerCase();
            const iconMap = {
                'pdf': 'fas fa-file-pdf text-danger',
                'doc': 'fas fa-file-word text-primary',
                'docx': 'fas fa-file-word text-primary',
                'xls': 'fas fa-file-excel text-success',
                'xlsx': 'fas fa-file-excel text-success',
                'ppt': 'fas fa-file-powerpoint text-warning',
                'pptx': 'fas fa-file-powerpoint text-warning',
                'jpg': 'fas fa-file-image text-success',
                'jpeg': 'fas fa-file-image text-success',
                'png': 'fas fa-file-image text-success',
                'gif': 'fas fa-file-image text-success',
                'zip': 'fas fa-file-archive text-warning',
                'rar': 'fas fa-file-archive text-warning',
                'exe': 'fas fa-cog text-secondary',
                'txt': 'fas fa-file-alt text-secondary',
                'js': 'fas fa-file-code text-warning',
                'html': 'fas fa-file-code text-danger',
                'css': 'fas fa-file-code text-info'
            };

            return iconMap[extension] || 'fas fa-file text-secondary';
        }

        updateToolbarButtons() {
            const hasSelection = selectedItems.length > 0;
            const singleSelection = selectedItems.length === 1;
            
            // Hide all action groups by default
            $('.common-actions, .file-actions, .folder-actions').hide();
            
            // Show clipboard actions if clipboard has items
            if (clipboard.items && clipboard.items.length > 0) {
                $('.clipboard-actions').show();
                $('#clipboard-count').text(clipboard.items.length);
                
                // Update paste button text based on action
                const actionText = clipboard.action === 'move' ? 'Move Here' : 'Paste';
                $('#btn-paste').html(`<i class="fas fa-paste"></i> ${actionText} <span class="badge badge-light ml-1" id="clipboard-count">${clipboard.items.length}</span>`);
            } else {
                $('.clipboard-actions').hide();
            }
            
            if (!hasSelection) {
                return;
            }
            
            // Always show common actions when something is selected
            $('.common-actions').show();
            
            if (singleSelection) {
                const $selectedItem = $('.file-item.selected');
                const itemType = $selectedItem.data('type');
                
                if (itemType === 'file') {
                    $('.file-actions').show();
                } else if (itemType === 'folder') {
                    $('.folder-actions').show();
                }
            } else {
                // Multiple selection - show limited options
                const hasFiles = selectedItems.some(path => {
                    const $item = $(`.file-item[data-path="${path}"]`);
                    return $item.data('type') === 'file';
                });
                
                const hasFolders = selectedItems.some(path => {
                    const $item = $(`.file-item[data-path="${path}"]`);
                    return $item.data('type') === 'folder';
                });
                
                // For mixed selection, show basic file actions
                if (hasFiles) {
                    $('.file-actions').show();
                    // Hide download for mixed selections
                    if (hasFolders) {
                        $('#btn-download').hide();
                    }
                }
            }
        }

        showUploadZone() {
            $('#upload-zone').toggle();
        }

        initializeUploadZone() {
            const $uploadZone = $('#upload-zone');
            const $fileInput = $('#file-input');

            // Click to upload
            $uploadZone.on('click', () => $fileInput.click());

            // File input change
            $fileInput.on('change', (e) => {
                this.handleFileUpload(e.target.files);
            });

            // Drag and drop
            $uploadZone.on('dragover', (e) => {
                e.preventDefault();
                $uploadZone.addClass('dragover');
            });

            $uploadZone.on('dragleave', () => {
                $uploadZone.removeClass('dragover');
            });

            $uploadZone.on('drop', (e) => {
                e.preventDefault();
                $uploadZone.removeClass('dragover');
                this.handleFileUpload(e.originalEvent.dataTransfer.files);
            });
        }

        handleFileUpload(files) {
            Array.from(files).forEach(file => {
                const operationId = this.createProgressItem(file.name, 'upload', this.formatFileSize(file.size));
                this.simulateFileOperation(operationId, 'upload', file.name);
            });
        }

        showNewFolderModal() {
            $('#folder-name').val('');
            $('#new-folder-modal').modal('show');
        }

        createFolder() {
            const folderName = $('#folder-name').val().trim();
            
            if (!folderName) {
                toastr.error('Please enter a folder name');
                return;
            }

            // Simulate folder creation
            console.log('Creating folder:', folderName);
            toastr.success(`Folder "${folderName}" created successfully!`);
            
            $('#new-folder-modal').modal('hide');
            this.loadDirectory(currentPath); // Refresh directory
        }

        renameSelected() {
            if (selectedItems.length !== 1) return;

            const item = $('.file-item.selected');
            const currentName = item.data('name');
            
            $('#new-name').val(currentName);
            $('#rename-modal').modal('show');
        }

        confirmRename() {
            const newName = $('#new-name').val().trim();
            
            if (!newName) {
                toastr.error('Please enter a new name');
                return;
            }

            // Simulate rename
            console.log('Renaming to:', newName);
            toastr.success(`Item renamed to "${newName}" successfully!`);
            
            $('#rename-modal').modal('hide');
            this.loadDirectory(currentPath); // Refresh directory
        }

        copySelected() {
            if (selectedItems.length === 0) return;
            
            clipboard = { 
                items: [...selectedItems], 
                action: 'copy',
                sourcePath: currentPath
            };
            
            toastr.success(`${selectedItems.length} item(s) copied to clipboard`);
            this.updateToolbarButtons();
            
            // Clear selection after copy
            $('.file-item').removeClass('selected');
            selectedItems = [];
        }

        moveSelected() {
            if (selectedItems.length === 0) return;
            
            clipboard = { 
                items: [...selectedItems], 
                action: 'move',
                sourcePath: currentPath
            };
            
            toastr.success(`${selectedItems.length} item(s) cut to clipboard`);
            this.updateToolbarButtons();
            
            // Visual feedback for cut items
            selectedItems.forEach(itemPath => {
                const $item = $(`.file-item[data-path="${itemPath}"]`);
                $item.addClass('cut-item').css('opacity', '0.5');
            });
            
            // Clear selection after cut
            $('.file-item').removeClass('selected');
            selectedItems = [];
        }

        pasteItems() {
            if (!clipboard.items || clipboard.items.length === 0) {
                toastr.error('No items in clipboard');
                return;
            }

            const action = clipboard.action;
            const itemCount = clipboard.items.length;
            const itemNames = clipboard.items.map(path => path.split('/').pop()).join(', ');

            $.confirm({
                title: `${action === 'move' ? 'Move' : 'Copy'} Items`,
                content: `Are you sure you want to ${action} ${itemCount} item(s) (${itemNames}) to this location?`,
                type: action === 'move' ? 'orange' : 'blue',
                theme: 'dark',
                buttons: {
                    confirm: {
                        text: action === 'move' ? 'Move' : 'Copy',
                        btnClass: action === 'move' ? 'btn-warning' : 'btn-primary',
                        action: () => {
                            this.executePaste();
                        }
                    },
                    cancel: {
                        text: 'Cancel',
                        btnClass: 'btn-secondary'
                    }
                }
            });
        }

        executePaste() {
            const action = clipboard.action;
            const itemCount = clipboard.items.length;

            // Simulate paste operation
            console.log(`${action}ing items:`, clipboard.items);
            console.log(`From: ${clipboard.sourcePath} To: ${currentPath}`);

            if (action === 'move') {
                // Remove cut styling from source items
                $('.cut-item').css('opacity', '1').removeClass('cut-item');
                toastr.success(`${itemCount} item(s) moved successfully!`);
                clipboard = { items: [], action: null, sourcePath: null };
            } else {
                toastr.success(`${itemCount} item(s) copied successfully!`);
                // Keep clipboard for multiple pastes with copy
            }

            this.loadDirectory(currentPath); // Refresh directory
            this.updateToolbarButtons();
        }

        clearClipboard() {
            if (!clipboard.items || clipboard.items.length === 0) {
                toastr.info('Clipboard is already empty');
                return;
            }

            // Remove cut styling
            $('.cut-item').css('opacity', '1').removeClass('cut-item');
            
            clipboard = { items: [], action: null, sourcePath: null };
            toastr.info('Clipboard cleared');
            this.updateToolbarButtons();
        }

        deleteSelected() {
            if (selectedItems.length === 0) return;

            const itemNames = selectedItems.map(path => path.split('/').pop()).join(', ');

            $.confirm({
                title: 'Confirm Delete',
                content: `Are you sure you want to permanently delete ${selectedItems.length} item(s)?<br><br><strong>Items:</strong> ${itemNames}<br><br><span style="color: #dc3545;">This action cannot be undone!</span>`,
                type: 'red',
                theme: 'dark',
                buttons: {
                    confirm: {
                        text: 'Delete Permanently',
                        btnClass: 'btn-danger',
                        action: () => {
                            // Simulate delete
                            console.log('Deleting:', selectedItems);
                            toastr.success(`${selectedItems.length} item(s) deleted successfully!`);
                            
                            // Clear selection
                            $('.file-item').removeClass('selected');
                            selectedItems = [];
                            
                            this.loadDirectory(currentPath); // Refresh directory
                            this.updateToolbarButtons();
                        }
                    },
                    cancel: {
                        text: 'Cancel',
                        btnClass: 'btn-secondary'
                    }
                }
            });
        }

        downloadSelected() {
            selectedItems.forEach(itemPath => {
                this.downloadFile(itemPath);
            });
        }

        downloadFile(filePath) {
            const fileName = filePath.split('/').pop();
            const operationId = this.createProgressItem(fileName, 'download');
            this.simulateFileOperation(operationId, 'download', fileName);
        }

        showFileDetails() {
            if (selectedItems.length !== 1) {
                toastr.error('Please select exactly one file to view details');
                return;
            }

            const $selectedItem = $('.file-item.selected');
            const fileName = $selectedItem.data('name');
            const filePath = $selectedItem.data('path');
            const fileType = $selectedItem.data('type');
            
            // Get file extension and icon
            const extension = fileName.split('.').pop().toLowerCase();
            const iconClass = this.getFileIcon(fileName, fileType);
            
            // Mock file data (in real app, this would come from server)
            const fileData = this.generateMockFileData(fileName, filePath, fileType, extension);
            
            // Update modal content
            this.populateDetailsModal(fileData, iconClass);
            
            // Show modal
            $('#file-details-modal').modal('show');
        }

        generateMockFileData(fileName, filePath, fileType, extension) {
            const now = new Date();
            const sizes = ['1.2 MB', '854 KB', '3.5 MB', '125 KB', '2.1 GB', '45 KB'];
            const randomSize = sizes[Math.floor(Math.random() * sizes.length)];
            
            return {
                name: fileName,
                type: this.getFileTypeDescription(extension),
                size: fileType === 'folder' ? '-' : randomSize,
                location: filePath.substring(0, filePath.lastIndexOf('/')),
                created: new Date(now.getTime() - Math.random() * 365 * 24 * 60 * 60 * 1000).toLocaleDateString(),
                modified: new Date(now.getTime() - Math.random() * 30 * 24 * 60 * 60 * 1000).toLocaleDateString(),
                accessed: new Date(now.getTime() - Math.random() * 7 * 24 * 60 * 60 * 1000).toLocaleDateString(),
                permissions: 'Read Only',
                description: this.getFileDescription(extension)
            };
        }

        getFileTypeDescription(extension) {
            const typeMap = {
                'pdf': 'Adobe PDF Document',
                'doc': 'Microsoft Word Document',
                'docx': 'Microsoft Word Document',
                'xls': 'Microsoft Excel Spreadsheet',
                'xlsx': 'Microsoft Excel Spreadsheet',
                'ppt': 'Microsoft PowerPoint Presentation',
                'pptx': 'Microsoft PowerPoint Presentation',
                'jpg': 'JPEG Image',
                'jpeg': 'JPEG Image',
                'png': 'PNG Image',
                'gif': 'GIF Image',
                'zip': 'ZIP Archive',
                'rar': 'RAR Archive',
                'exe': 'Executable File',
                'txt': 'Text Document',
                'js': 'JavaScript File',
                'html': 'HTML Document',
                'css': 'CSS Stylesheet'
            };
            return typeMap[extension] || 'Unknown File Type';
        }

        getFileDescription(extension) {
            const descriptions = {
                'pdf': 'Portable Document Format file for sharing documents across platforms',
                'doc': 'Word processing document with text, images, and formatting',
                'docx': 'Modern Word document with enhanced features and compatibility',
                'xls': 'Spreadsheet file for data analysis and calculations',
                'xlsx': 'Modern Excel spreadsheet with enhanced features',
                'jpg': 'Compressed image format commonly used for photos',
                'png': 'Lossless image format with transparency support',
                'txt': 'Plain text file containing unformatted text',
                'js': 'JavaScript source code file for web development'
            };
            return descriptions[extension] || 'File contains data in a specific format';
        }

        populateDetailsModal(fileData, iconClass) {
            // Update icon
            $('#file-preview-icon i').attr('class', iconClass + ' fa-5x');
            
            // Always show icon, never load actual images
            $('#file-preview-image').hide();
            $('#file-preview-icon').show();
            
            // Update details
            $('#detail-name').text(fileData.name);
            $('#detail-type').text(fileData.type);
            $('#detail-size').text(fileData.size);
            $('#detail-location').text(fileData.location || '/');
            $('#detail-created').text(fileData.created);
            $('#detail-modified').text(fileData.modified);
            $('#detail-accessed').text(fileData.accessed);
            $('#detail-permissions').text(fileData.permissions);
            
            // Show/hide description
            if (fileData.description) {
                $('#file-description').text(fileData.description);
                $('#file-description-section').show();
            } else {
                $('#file-description-section').hide();
            }
        }

        downloadFromDetails() {
            const $selectedItem = $('.file-item.selected');
            if ($selectedItem.length) {
                const filePath = $selectedItem.data('path');
                $('#file-details-modal').modal('hide');
                this.downloadFile(filePath);
            }
        }

        openSelectedItems() {
            selectedItems.forEach(itemPath => {
                const $item = $(`.file-item[data-path="${itemPath}"]`);
                if ($item.data('type') === 'folder') {
                    this.navigateTo(itemPath);
                } else {
                    this.downloadFile(itemPath);
                }
            });
        }

        searchFiles(query) {
            const $items = $('.file-item');
            
            if (!query.trim()) {
                $items.show();
                return;
            }

            $items.each(function () {
                const fileName = $(this).data('name').toLowerCase();
                const matches = fileName.includes(query.toLowerCase());
                $(this).toggle(matches);
            });
        }

        handleKeyboard(e) {
            // Ctrl+A - Select all
            if (e.ctrlKey && e.key === 'a') {
                e.preventDefault();
                $('.file-item').addClass('selected');
                selectedItems = $('.file-item').map(function () {
                    return $(this).data('path');
                }).get();
                this.updateToolbarButtons();
            }

            // Delete key
            if (e.key === 'Delete' && selectedItems.length > 0) {
                this.deleteSelected();
            }

            // Enter key - open selected items
            if (e.key === 'Enter' && selectedItems.length > 0) {
                this.openSelectedItems();
            }

            // Escape key - clear selection
            if (e.key === 'Escape') {
                $('.file-item').removeClass('selected');
                selectedItems = [];
                this.updateToolbarButtons();
                this.hideContextMenu();
            }

            // Ctrl+C - Copy
            if (e.ctrlKey && e.key === 'c' && selectedItems.length > 0) {
                this.copySelected();
            }

            // Ctrl+X - Cut
            if (e.ctrlKey && e.key === 'x' && selectedItems.length > 0) {
                this.moveSelected();
            }
        }

        // Progress Bar Management Methods
        createProgressItem(fileName, operation, size = null) {
            const operationId = `op_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
            const progressHtml = `
                <div class="progress-item mb-3" id="${operationId}">
                    <div class="d-flex justify-content-between align-items-center mb-1">
                        <div class="progress-info">
                            <i class="fas fa-${operation === 'upload' ? 'upload' : 'download'} mr-2"></i>
                            <span class="file-name">${fileName}</span>
                            ${size ? `<small class="text-muted ml-2">(${size})</small>` : ''}
                        </div>
                        <div class="progress-actions">
                            <span class="progress-percentage">0%</span>
                            <button type="button" class="btn btn-sm btn-outline-danger ml-2" onclick="fileManager.cancelOperation('${operationId}')">
                                <i class="fas fa-times"></i>
                            </button>
                        </div>
                    </div>
                    <div class="progress" style="height: 8px;">
                        <div class="progress-bar progress-bar-striped progress-bar-animated bg-${operation === 'upload' ? 'success' : 'info'}" 
                             role="progressbar" style="width: 0%" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100">
                        </div>
                    </div>
                    <div class="progress-status">
                        <small class="text-muted">
                            <span class="status-text">Starting ${operation}...</span>
                            <span class="speed-info ml-2"></span>
                        </small>
                    </div>
                </div>
            `;

            // Show progress container if hidden
            const $progressContainer = $('#progress-container');
            if ($progressContainer.is(':hidden')) {
                $progressContainer.slideDown();
            }

            // Add progress item
            $('#progress-list').append(progressHtml);

            return operationId;
        }

        updateProgress(operationId, percentage, speed = null) {
            const $progressItem = $(`#${operationId}`);
            if ($progressItem.length === 0) return;

            const $progressBar = $progressItem.find('.progress-bar');
            const $percentage = $progressItem.find('.progress-percentage');
            const $statusText = $progressItem.find('.status-text');
            const $speedInfo = $progressItem.find('.speed-info');

            // Update progress bar
            $progressBar.css('width', `${percentage}%`).attr('aria-valuenow', percentage);
            $percentage.text(`${Math.round(percentage)}%`);

            // Update status text
            if (percentage < 100) {
                $statusText.text(`${$progressItem.find('.fa-upload').length ? 'Uploading' : 'Downloading'}...`);
            } else {
                $statusText.text(`${$progressItem.find('.fa-upload').length ? 'Upload' : 'Download'} complete`);
                $progressBar.removeClass('progress-bar-animated');
            }

            // Update speed info
            if (speed) {
                $speedInfo.text(`${speed}/s`);
            }

            // Auto-remove completed operations after 3 seconds
            if (percentage >= 100) {
                setTimeout(() => {
                    this.removeOperation(operationId);
                }, 3000);
            }
        }

        setOperationError(operationId, errorMessage) {
            const $progressItem = $(`#${operationId}`);
            if ($progressItem.length === 0) return;

            const $progressBar = $progressItem.find('.progress-bar');
            const $statusText = $progressItem.find('.status-text');

            $progressBar.removeClass('bg-success bg-info progress-bar-animated').addClass('bg-danger');
            $statusText.text(`Error: ${errorMessage}`).addClass('text-danger');

            // Auto-remove error operations after 5 seconds
            setTimeout(() => {
                this.removeOperation(operationId);
            }, 5000);
        }

        cancelOperation(operationId) {
            this.setOperationError(operationId, 'Operation cancelled');
        }

        removeOperation(operationId) {
            const $progressItem = $(`#${operationId}`);
            $progressItem.slideUp(() => {
                $progressItem.remove();

                // Hide progress container if no operations
                if ($('#progress-list').children().length === 0) {
                    $('#progress-container').slideUp();
                }
            });
        }

        simulateFileOperation(operationId, operation, fileName) {
            let progress = 0;
            const increment = Math.random() * 15 + 5; // Random increment between 5-20%
            const baseDelay = 200; // Base delay in ms
            
            const updateProgress = () => {
                progress += increment + (Math.random() * 10 - 5); // Add some randomness
                progress = Math.min(progress, 100);
                
                // Generate fake speed
                const speeds = ['1.2 MB', '850 KB', '2.1 MB', '650 KB', '1.8 MB'];
                const randomSpeed = speeds[Math.floor(Math.random() * speeds.length)];
                
                this.updateProgress(operationId, progress, randomSpeed);
                
                if (progress < 100) {
                    // Variable delay to simulate network fluctuations
                    const delay = baseDelay + (Math.random() * 300);
                    setTimeout(updateProgress, delay);
                } else {
                    // Operation completed
                    toastr.success(`${fileName} ${operation}ed successfully!`);
                    
                    // Refresh directory if upload
                    if (operation === 'upload') {
                        setTimeout(() => {
                            this.loadDirectory(currentPath);
                        }, 1000);
                    }
                }
            };
            
            // Start progress updates
            setTimeout(updateProgress, 100);
        }

        formatFileSize(bytes) {
            if (bytes === 0) return '0 Bytes';
            const k = 1024;
            const sizes = ['Bytes', 'KB', 'MB', 'GB'];
            const i = Math.floor(Math.log(bytes) / Math.log(k));
            return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
        }
    }

    // Initialize File Manager
    const fileManager = new FileManager();
});