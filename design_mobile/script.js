/**
 * CenIT TOC CRM - Mobile App Interactive Prototype & Showcase Controller
 * Tích hợp điều khiển Simulator, chuyển đổi chủ đề Dark/Light, Modal xem ảnh Mockup Full HD
 */

// Chuyển đổi màn hình Simulator
function switchScreen(screenId, navEl) {
    // Ẩn tất cả các màn hình trong viewport điện thoại
    const allScreens = document.querySelectorAll('.screen-page');
    allScreens.forEach(page => page.classList.remove('active'));

    // Gỡ active trên danh mục sidebar
    const allNavItems = document.querySelectorAll('.screen-nav-item');
    allNavItems.forEach(item => item.classList.remove('active'));

    // Hiển thị màn hình mong muốn
    const targetScreen = document.getElementById(screenId);
    if (targetScreen) {
        targetScreen.classList.add('active');
        // Cuộn nội dung lên đầu
        const scrollArea = targetScreen.querySelector('.app-scrollable-content');
        if (scrollArea) {
            scrollArea.scrollTop = 0;
        }
    }

    // Đánh dấu active trên sidebar button
    if (navEl) {
        navEl.classList.add('active');
    } else {
        const matchedNav = document.querySelector(`.screen-nav-item[onclick*="${screenId}"]`);
        if (matchedNav) matchedNav.classList.add('active');
    }

    // Đồng bộ trạng thái Bottom Navigation Bar
    updateBottomNavState(screenId);
}

// Chuyển đổi màn hình bằng ID (từ Bottom Tabs hoặc nút hành động trong App)
function switchScreenById(screenId) {
    switchScreen(screenId, null);
}

// Đồng bộ trạng thái của 5 Bottom Tabs
function updateBottomNavState(screenId) {
    const bottomTabs = document.querySelectorAll('.nav-tab');
    bottomTabs.forEach(tab => tab.classList.remove('active'));

    if (screenId === 'scr-dashboard') {
        if (bottomTabs[0]) bottomTabs[0].classList.add('active');
    } else if (screenId === 'scr-sales-list' || screenId === 'scr-sales-detail') {
        if (bottomTabs[1]) bottomTabs[1].classList.add('active');
    } else if (screenId === 'scr-wbs-checklist' || screenId === 'scr-report-modal') {
        if (bottomTabs[2]) bottomTabs[2].classList.add('active');
    } else if (screenId === 'scr-shared-docs') {
        if (bottomTabs[3]) bottomTabs[3].classList.add('active');
    } else if (screenId === 'scr-login') {
        if (bottomTabs[4]) bottomTabs[4].classList.add('active');
    }
}

// Chuyển đổi giao diện Sáng / Tối cho Simulator
function setAppTheme(theme) {
    const phoneContainer = document.getElementById('phoneContainer');
    const btnDark = document.getElementById('btnThemeDark');
    const btnLight = document.getElementById('btnThemeLight');

    if (!phoneContainer) return;

    if (theme === 'light') {
        phoneContainer.classList.remove('theme-dark');
        phoneContainer.classList.add('theme-light');
        if (btnLight) btnLight.classList.add('active');
        if (btnDark) btnDark.classList.remove('active');
    } else {
        phoneContainer.classList.remove('theme-light');
        phoneContainer.classList.add('theme-dark');
        if (btnDark) btnDark.classList.add('active');
        if (btnLight) btnLight.classList.remove('active');
    }
}

// Chuyển tab giữa chế độ "Mô phỏng tương tác (Interactive)" và "Thư viện thiết kế UI Full HD (Gallery Showcase)"
function switchViewMode(mode) {
    const simulatorView = document.getElementById('viewSimulator');
    const galleryView = document.getElementById('viewGallery');
    const btnSim = document.getElementById('btnModeSimulator');
    const btnGal = document.getElementById('btnModeGallery');

    if (mode === 'gallery') {
        if (simulatorView) simulatorView.style.display = 'none';
        if (galleryView) galleryView.style.display = 'block';
        if (btnSim) btnSim.classList.remove('active');
        if (btnGal) btnGal.classList.add('active');
    } else {
        if (simulatorView) simulatorView.style.display = 'flex';
        if (galleryView) galleryView.style.display = 'none';
        if (btnSim) btnSim.classList.add('active');
        if (btnGal) btnGal.classList.remove('active');
    }
}

// Mở Modal xem ảnh UI Mockup phóng to chất lượng cao
function openImageModal(imgSrc, title, desc) {
    let modal = document.getElementById('galleryImageModal');
    if (!modal) {
        modal = document.createElement('div');
        modal.id = 'galleryImageModal';
        modal.className = 'gallery-lightbox-modal';
        modal.innerHTML = `
            <div class="lightbox-overlay" onclick="closeImageModal()"></div>
            <div class="lightbox-content">
                <button class="lightbox-close" onclick="closeImageModal()"><i class="fa-solid fa-xmark"></i></button>
                <div class="lightbox-img-wrapper">
                    <img id="lightboxImg" src="" alt="UI Mockup Preview">
                </div>
                <div class="lightbox-info">
                    <h3 id="lightboxTitle"></h3>
                    <p id="lightboxDesc"></p>
                    <a id="lightboxDownload" href="" target="_blank" class="btn-download-hd"><i class="fa-solid fa-arrow-up-right-from-square"></i> Mở ảnh gốc Full-HD</a>
                </div>
            </div>
        `;
        document.body.appendChild(modal);
    }

    document.getElementById('lightboxImg').src = imgSrc;
    document.getElementById('lightboxTitle').textContent = title;
    document.getElementById('lightboxDesc').textContent = desc;
    document.getElementById('lightboxDownload').href = imgSrc;

    modal.style.display = 'flex';
}

function closeImageModal() {
    const modal = document.getElementById('galleryImageModal');
    if (modal) {
        modal.style.display = 'none';
    }
}

// Micro-interaction: Đánh dấu hoàn thành WBS Task
function toggleWbsTask(checkboxEl) {
    const parentRow = checkboxEl.closest('.wbs-node-row');
    if (!parentRow) return;
    const taskName = parentRow.querySelector('.node-title');
    
    if (checkboxEl.checked) {
        if (taskName) {
            taskName.style.textDecoration = 'line-through';
            taskName.style.opacity = '0.6';
        }
        showToastNotification('✅ Đã cập nhật trạng thái hạng mục WBS');
    } else {
        if (taskName) {
            taskName.style.textDecoration = 'none';
            taskName.style.opacity = '1';
        }
    }
}

// Giả lập gửi trao đổi thảo luận
function sendDiscussionMessage() {
    const input = document.getElementById('discussionInput');
    const list = document.getElementById('chatMsgList');
    if (!input || !list || !input.value.trim()) return;

    const text = input.value.trim();
    const now = new Date();
    const timeStr = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    const msgItem = document.createElement('div');
    msgItem.className = 'chat-bubble bubble-me';
    msgItem.innerHTML = `
        <div class="bubble-meta">Bạn • ${timeStr}</div>
        <div class="bubble-body">${text}</div>
    `;

    list.appendChild(msgItem);
    input.value = '';
    list.scrollTop = list.scrollHeight;
    showToastNotification('💬 Đã gửi ý kiến trao đổi điều hành');
}

// Hiển thị Toast thông báo trong khung điện thoại
function showToastNotification(message) {
    let toast = document.getElementById('phoneToastNotification');
    if (!toast) {
        toast = document.createElement('div');
        toast.id = 'phoneToastNotification';
        toast.className = 'phone-toast-box';
        const phoneScreen = document.querySelector('.phone-screen');
        if (phoneScreen) {
            phoneScreen.appendChild(toast);
        } else {
            document.body.appendChild(toast);
        }
    }

    toast.innerHTML = message;
    toast.classList.add('visible');

    setTimeout(() => {
        toast.classList.remove('visible');
    }, 2400);
}

// Tự động cập nhật đồng hồ trên Status Bar
document.addEventListener('DOMContentLoaded', () => {
    const timeElements = document.querySelectorAll('.status-time');
    const updateTime = () => {
        const now = new Date();
        const str = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false });
        timeElements.forEach(el => el.textContent = str);
    };
    updateTime();
    setInterval(updateTime, 30000);

    // Gắn sự kiện Enter cho ô chat
    const chatInput = document.getElementById('discussionInput');
    if (chatInput) {
        chatInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') sendDiscussionMessage();
        });
    }

    // Đóng modal khi bấm ESC
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') closeImageModal();
    });
});
