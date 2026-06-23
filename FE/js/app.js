// Configuration
const API_BASE_URL = "";

// API Fetch Helper
async function apiFetch(endpoint, options = {}) {
    const url = endpoint.startsWith("http") ? endpoint : `${API_BASE_URL}/api/${endpoint.replace(/^\//, "")}`;
    
    // Ensure credentials: 'include' is set for Cookie sharing
    options.credentials = 'include';
    
    if (options.body && typeof options.body === 'object' && !(options.body instanceof FormData)) {
        options.body = JSON.stringify(options.body);
        options.headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };
    }
    
    try {
        const response = await fetch(url, options);
        if (response.status === 401) {
            // Handle unauthorized - clear local state
            localStorage.removeItem("user");
            const publicPages = ["login.html", "register.html", "index.html", "shop.html", "detail.html"];
            const isPublic = publicPages.some(p => window.location.pathname.endsWith(p));
            if (!isPublic) {
                const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
                window.location.href = `login.html?returnUrl=${returnUrl}`;
            }
        }
        return response;
    } catch (error) {
        console.error("API Call error:", error);
        throw error;
    }
}

// User session management
function getCurrentUser() {
    try {
        const userData = localStorage.getItem("user");
        return userData ? JSON.parse(userData) : null;
    } catch (e) {
        return null;
    }
}

async function checkAuthStatus() {
    try {
        const res = await apiFetch("AuthApi/status");
        if (res.ok) {
            const data = await res.json();
            if (data.isAuthenticated) {
                localStorage.setItem("user", JSON.stringify(data.user));
                return data.user;
            }
        }
    } catch (e) {
        console.warn("Could not check authentication status.");
    }
    localStorage.removeItem("user");
    return null;
}

// ====================================================
// CART MANAGEMENT - Yêu cầu đăng nhập để sử dụng
// ====================================================

async function addToCart(variantId, quantity = 1) {
    const user = getCurrentUser();
    if (!user) {
        // Chưa đăng nhập: chuyển đến trang login
        const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
        window.location.href = `login.html?returnUrl=${returnUrl}`;
        return { success: false, message: "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng." };
    }
    try {
        const res = await apiFetch("CartApi/add", {
            method: "POST",
            body: { variantId, quantity }
        });
        const data = await res.json();
        return { success: res.ok, message: data.message };
    } catch (e) {
        return { success: false, message: "Lỗi kết nối máy chủ." };
    }
}

async function getCartItems() {
    const user = getCurrentUser();
    if (!user) return [];
    try {
        const res = await apiFetch("CartApi");
        if (res.ok) return await res.json();
    } catch (e) {
        console.error("Error loading cart:", e);
    }
    return [];
}

async function updateCartQuantity(cartItemId, variantId, quantity) {
    const user = getCurrentUser();
    if (!user) {
        window.location.href = "login.html";
        return { success: false, message: "Vui lòng đăng nhập." };
    }
    const res = await apiFetch("CartApi/update", {
        method: "POST",
        body: { cartItemId, variantId, quantity }
    });
    const data = await res.json();
    return { success: res.ok, message: data.message };
}

async function removeCartItem(cartItemId, variantId) {
    const user = getCurrentUser();
    if (!user) {
        window.location.href = "login.html";
        return { success: false, message: "Vui lòng đăng nhập." };
    }
    const res = await apiFetch("CartApi/remove", {
        method: "POST",
        body: { cartItemId }
    });
    const data = await res.json();
    return { success: res.ok, message: data.message };
}

async function updateCartCountBadge() {
    const badge = document.getElementById("cart-badge");
    if (!badge) return;

    const user = getCurrentUser();
    if (!user) {
        badge.style.display = "none";
        return;
    }

    let count = 0;
    try {
        const res = await apiFetch("CartApi");
        if (res.ok) {
            const items = await res.json();
            count = items.reduce((sum, item) => sum + item.quantity, 0);
        }
    } catch (e) {
        console.warn("Could not retrieve cart count.");
    }

    badge.innerText = count;
    badge.style.display = count > 0 ? "flex" : "none";
}

// ====================================================
// Logo Typewriter Animation - DMTShop (Deprecated in favor of premium CSS SVG draw)
// ====================================================
function startLogoTypewriter() {
    // Không làm gì, đã thay bằng logo SVG vẽ nét premium
}

// Global UI Rendering Helpers
function injectHeader() {
    const headerHtml = `
    <style>
    @import url('https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;900&display=swap');

    #dmt-logo-link {
        font-family: 'Outfit', sans-serif;
        display: flex;
        align-items: center;
        text-decoration: none;
        transition: transform 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
    }
    .dmt-logo-icon {
        stroke-dasharray: 100;
        stroke-dashoffset: 100;
        animation: dmtDrawLine 1.5s cubic-bezier(0.4, 0, 0.2, 1) forwards;
        transition: transform 0.4s cubic-bezier(0.34, 1.56, 0.64, 1), color 0.4s ease;
    }
    @keyframes dmtDrawLine {
        to {
            stroke-dashoffset: 0;
        }
    }
    .dmt-logo-bold {
        font-size: 1.65rem;
        letter-spacing: -0.03em;
    }
    .dmt-logo-bold .dmt-bounce-item {
        font-weight: 900;
        background: linear-gradient(120deg, #4f46e5 0%, #7c3aed 50%, #db2777 100%);
        background-size: 200% 200%;
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        background-clip: text;
        animation: dmtGradientShift 5s ease infinite, dmtWaveBounce 4.5s ease-in-out infinite;
        transition: filter 0.4s ease;
    }
    .dmt-logo-light {
        font-size: 1.65rem;
        margin-left: 1px;
    }
    .dmt-logo-light .dmt-bounce-item {
        font-weight: 300;
        color: #1e293b;
        animation: dmtWaveBounce 4.5s ease-in-out infinite;
        transition: color 0.4s ease;
    }
    @keyframes dmtGradientShift {
        0% { background-position: 0% 50%; }
        50% { background-position: 100% 50%; }
        100% { background-position: 0% 50%; }
    }
    .dmt-bounce-item {
        display: inline-block;
    }
    @keyframes dmtWaveBounce {
        0%, 15%, 100% {
            transform: translateY(0);
        }
        7.5% {
            transform: translateY(-8px);
        }
    }
    #dmt-logo-link:hover {
        transform: translateY(-1px);
    }
    #dmt-logo-link:hover .dmt-logo-icon {
        transform: rotate(-12deg) scale(1.1);
        color: #db2777;
    }
    #dmt-logo-link:hover .dmt-logo-bold .dmt-bounce-item {
        filter: drop-shadow(0 0 10px rgba(124, 58, 237, 0.5));
    }
    #dmt-logo-link:hover .dmt-logo-light .dmt-bounce-item {
        color: #7c3aed;
    }
    </style>
    <header class="sticky top-0 z-50 bg-white/80 backdrop-blur-md border-b border-gray-100 transition-all duration-300">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div class="flex justify-between items-center h-16">
                <!-- Logo DMTShop Animated -->
                <div class="flex-shrink-0">
                    <a href="index.html" id="dmt-logo-link">
                        <!-- SVG Icon Móc áo vẽ nét -->
                        <span class="dmt-bounce-item" style="animation-delay: 0s;">
                            <svg class="dmt-logo-icon h-7 w-7 mr-1.5 text-indigo-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                                <path d="M12 2a3 3 0 0 1 3 3c0 .82-.4 1.5-1 2H10c-.6-.5-1-1.18-1-2a3 3 0 0 1 3-3z"/>
                                <path d="M21 16.5A2.5 2.5 0 0 1 18.5 19H5.5A2.5 2.5 0 0 1 3 16.5c0-1.8 1-3.4 2.5-4.2l6-3.3a1 1 0 0 1 1 0l6 3.3c1.5.8 2.5 2.4 2.5 4.2z"/>
                            </svg>
                        </span>
                        <span class="dmt-logo-bold">
                            <span class="dmt-bounce-item" style="animation-delay: 0.15s;">D</span>
                            <span class="dmt-bounce-item" style="animation-delay: 0.3s;">M</span>
                            <span class="dmt-bounce-item" style="animation-delay: 0.45s;">T</span>
                        </span>
                        <span class="dmt-logo-light">
                            <span class="dmt-bounce-item" style="animation-delay: 0.6s;">S</span>
                            <span class="dmt-bounce-item" style="animation-delay: 0.75s;">h</span>
                            <span class="dmt-bounce-item" style="animation-delay: 0.9s;">o</span>
                            <span class="dmt-bounce-item" style="animation-delay: 1.05s;">p</span>
                        </span>
                    </a>
                </div>
                
                <!-- Nav Links -->
                <nav class="hidden md:flex space-x-8">
                    <a href="index.html" class="text-gray-600 hover:text-indigo-600 px-3 py-2 text-sm font-medium transition">Trang Chủ</a>
                    <a href="shop.html" class="text-gray-600 hover:text-indigo-600 px-3 py-2 text-sm font-medium transition">Cửa Hàng</a>
                </nav>
                
                <!-- Actions -->
                <div class="flex items-center space-x-4">
                    <!-- Search Bar -->
                    <form onsubmit="event.preventDefault(); const q = this.querySelector('input').value; if(q) window.location.href='shop.html?search='+encodeURIComponent(q);" class="hidden sm:flex relative text-gray-600">
                        <input type="search" placeholder="Tìm kiếm..." class="bg-gray-100 h-10 px-5 pr-10 rounded-full text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 w-40 lg:w-64 transition-all border border-transparent focus:bg-white focus:border-indigo-200">
                        <button type="submit" class="absolute right-0 top-0 mt-2.5 mr-4 text-gray-400 hover:text-indigo-600 transition">
                            <i class="fa-solid fa-magnifying-glass"></i>
                        </button>
                    </form>

                    <!-- Cart -->
                    <a href="cart.html" class="relative p-2 text-gray-600 hover:text-indigo-600 transition">
                        <i class="fa-solid fa-bag-shopping text-xl"></i>
                        <span id="cart-badge" class="absolute -top-1 -right-1 bg-gradient-to-r from-pink-500 to-rose-500 text-white rounded-full text-xxs w-5 h-5 flex items-center justify-center font-bold" style="display: none;">0</span>
                    </a>
                    
                    <!-- Auth -->
                    <div id="auth-actions" class="flex items-center space-x-2">
                        <a href="login.html" class="text-gray-600 hover:text-indigo-600 px-3 py-2 text-sm font-medium transition">Đăng Nhập</a>
                        <a href="register.html" class="bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-full text-sm font-medium transition shadow-md shadow-indigo-100">Đăng Ký</a>
                    </div>
                </div>
            </div>
        </div>
    </header>
    `;
    
    document.body.insertAdjacentHTML('afterbegin', headerHtml);
    
    // Update Auth buttons
    const user = getCurrentUser();
    const authActions = document.getElementById("auth-actions");
    if (user && authActions) {
        const isAdmin = user.roles && user.roles.includes("Admin");
        authActions.innerHTML = `
            <div class="relative group">
                <button class="flex items-center space-x-2 p-2 text-gray-700 hover:text-indigo-600 transition">
                    <i class="fa-regular fa-user text-lg"></i>
                    <span class="text-sm font-medium max-w-28 truncate">${user.fullName || user.email}</span>
                </button>
                <div class="absolute right-0 w-48 mt-1 origin-top-right bg-white rounded-xl shadow-xl border border-gray-100 divide-y divide-gray-50 opacity-0 invisible group-hover:opacity-100 group-hover:visible transition duration-150 z-50">
                    <div class="py-1">
                        ${!isAdmin ? `<a href="profile.html" class="flex items-center px-4 py-2.5 text-sm text-gray-700 hover:bg-gray-50"><i class="fa-regular fa-id-card mr-2 text-gray-400"></i>Hồ sơ của tôi</a>` : ""}
                        ${isAdmin ? `<a href="admin.html" class="flex items-center px-4 py-2.5 text-sm text-rose-600 hover:bg-gray-50 font-medium"><i class="fa-solid fa-user-gear mr-2 text-rose-400"></i>Trang quản trị</a>` : ""}
                    </div>
                    <div class="py-1">
                        <button onclick="handleLogout()" class="w-full flex items-center px-4 py-2.5 text-sm text-gray-500 hover:bg-rose-50 hover:text-rose-600 text-left"><i class="fa-solid fa-arrow-right-from-bracket mr-2 text-gray-400"></i>Đăng xuất</button>
                    </div>
                </div>
            </div>
        `;
    }
}

async function handleLogout() {
    try {
        await apiFetch("AuthApi/logout", { method: "POST" });
    } catch (e) {}
    localStorage.removeItem("user");
    window.location.href = "index.html";
}

function injectFooter() {
    const footerHtml = `
    <footer class="bg-gray-900 text-gray-400 pt-16 pb-8 border-t border-gray-800">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div class="grid grid-cols-1 md:grid-cols-3 gap-12 mb-12">
                <!-- Col 1: Thời trang nam -->
                <div>
                    <h3 class="text-sm font-semibold text-white uppercase tracking-wider mb-4">Thời trang nam DMTShop</h3>
                    <p class="mt-4 text-sm leading-relaxed text-gray-400">Hệ thống thời trang cho phái mạnh hàng đầu Việt Nam, hướng tới phong cách nam tính, lịch lãm và trẻ trung.</p>
                    <div class="mt-6 flex space-x-3">
                        <a href="#" class="w-8 h-8 rounded border border-gray-700 flex items-center justify-center hover:bg-gray-800 hover:text-white transition"><i class="fa-brands fa-facebook-f text-sm"></i></a>
                        <a href="#" class="w-8 h-8 rounded border border-gray-700 flex items-center justify-center hover:bg-gray-800 hover:text-white transition"><i class="fa-brands fa-twitter text-sm"></i></a>
                        <a href="#" class="w-8 h-8 rounded border border-gray-700 flex items-center justify-center hover:bg-gray-800 hover:text-white transition"><i class="fa-brands fa-instagram text-sm"></i></a>
                        <a href="#" class="w-8 h-8 rounded border border-gray-700 flex items-center justify-center hover:bg-gray-800 hover:text-white transition"><i class="fa-brands fa-tiktok text-sm"></i></a>
                        <a href="#" class="w-8 h-8 rounded border border-gray-700 flex items-center justify-center hover:bg-gray-800 hover:text-white transition"><i class="fa-brands fa-youtube text-sm"></i></a>
                    </div>
                    <h4 class="text-xs font-semibold text-white uppercase tracking-wider mt-8 mb-4">Phương thức thanh toán</h4>
                    <div class="flex flex-wrap gap-2">
                        <span class="px-2 py-1 bg-white text-blue-600 rounded text-xs font-bold">VNPAY</span>
                        <span class="px-2 py-1 bg-white text-green-500 rounded text-xs font-bold">ZaloPay</span>
                        <span class="px-2 py-1 bg-white text-indigo-600 rounded text-xs font-bold">VISA</span>
                        <span class="px-2 py-1 bg-white text-gray-800 rounded text-xs font-bold">MoMo</span>
                    </div>
                </div>

                <!-- Col 2: Thông tin liên hệ -->
                <div>
                    <h3 class="text-sm font-semibold text-white uppercase tracking-wider mb-4">Thông tin liên hệ</h3>
                    <ul class="space-y-3 text-sm text-gray-400">
                        <li><strong class="text-white font-medium">Địa chỉ:</strong> Tòa s1.02 Vinhome grandPark, Nguyễn Xiển, Q9, Tp.HCM.</li>
                        <li><strong class="text-white font-medium">Điện thoại:</strong> 0388.346.580</li>
                        <li><strong class="text-white font-medium">Email:</strong> nguyetle04112004@gmail.com</li>
                    </ul>
                    <h4 class="text-xs font-semibold text-white uppercase tracking-wider mt-8 mb-4">Phương thức vận chuyển</h4>
                    <div class="flex flex-wrap gap-2">
                        <span class="px-2 py-1 bg-white text-orange-500 rounded text-xs font-bold italic">GHN</span>
                        <span class="px-2 py-1 bg-white text-red-600 rounded text-xs font-bold italic">Ninja</span>
                        <span class="px-2 py-1 bg-white text-orange-600 rounded text-xs font-bold italic">AhaMove</span>
                        <span class="px-2 py-1 bg-white text-red-500 rounded text-xs font-bold italic">J&T Express</span>
                    </div>
                </div>

                <!-- Col 3: Đăng ký nhận tin -->
                <div>
                    <h3 class="text-sm font-semibold text-white uppercase tracking-wider mb-4">Đăng ký nhận tin</h3>
                    <p class="text-sm leading-relaxed text-gray-400 mb-4">Để cập nhật những sản phẩm mới, nhận thông tin ưu đãi đặc biệt và thông tin giảm giá khác.</p>
                    <form class="flex" onsubmit="event.preventDefault();">
                        <div class="relative flex-1">
                            <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                <i class="fa-regular fa-envelope text-gray-400"></i>
                            </div>
                            <input type="email" placeholder="Nhập email của bạn" class="w-full pl-10 pr-4 py-2.5 bg-gray-800 border border-gray-700 text-white text-sm rounded-l focus:outline-none focus:border-indigo-500 transition">
                        </div>
                        <button type="submit" class="px-5 py-2.5 bg-gray-200 hover:bg-gray-300 text-gray-900 text-sm font-bold uppercase rounded-r transition">Đăng ký</button>
                    </form>
                    <div class="mt-8">
                        <div class="inline-flex items-center space-x-3 border border-blue-500/30 p-2.5 rounded-lg bg-blue-500/10">
                            <i class="fa-solid fa-shield-check text-blue-500 text-3xl"></i>
                            <div class="flex flex-col text-left">
                                <span class="text-[10px] font-bold text-blue-400 uppercase leading-none">Đã thông báo</span>
                                <span class="text-xs font-black text-blue-500 uppercase leading-none mt-1.5 tracking-wider">Bộ công thương</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            
            <div class="border-t border-gray-800 pt-8 flex flex-col items-center text-xs text-gray-500">
                <p>&copy; 2026 DMTShop. All Rights Reserved.</p>
            </div>
        </div>
    </footer>
    `;
    const wrapper = document.createElement("div");
    wrapper.innerHTML = footerHtml;
    document.body.appendChild(wrapper.firstElementChild);
}

// Injects SignalR Chatbox Widget
function injectChatbox() {
    const chatboxHtml = `
    <!-- Floating Button -->
    <button id="chat-toggle" class="fixed bottom-6 right-6 bg-gradient-to-r from-indigo-600 to-violet-600 text-white rounded-full p-4 shadow-xl hover:scale-105 active:scale-95 transition-all duration-200 z-50 flex items-center justify-center w-14 h-14 border border-indigo-400">
        <i class="fa-regular fa-comment-dots text-2xl"></i>
    </button>
    
    <!-- Chat Window -->
    <div id="chat-window" class="fixed bottom-24 right-6 w-96 max-w-[calc(100vw-2rem)] h-[480px] bg-white rounded-2xl shadow-2xl border border-gray-150 flex flex-col overflow-hidden opacity-0 scale-95 pointer-events-none transition-all duration-300 z-50">
        <!-- Header -->
        <div class="bg-gradient-to-r from-indigo-600 to-violet-600 text-white p-4 flex justify-between items-center shadow">
            <div class="flex items-center space-x-3">
                <div class="w-2.5 h-2.5 bg-green-400 rounded-full animate-pulse"></div>
                <div>
                    <h3 class="font-bold text-sm tracking-wide text-white">Trợ lý ảo thông minh</h3>
                    <p class="text-[10px] text-indigo-100">Gợi ý sản phẩm tự động 24/7</p>
                </div>
            </div>
            <button id="chat-close" class="text-white hover:text-indigo-200 transition">
                <i class="fa-solid fa-xmark text-lg"></i>
            </button>
        </div>
        
        <!-- Messages Box -->
        <div id="chat-messages" class="flex-1 p-4 overflow-y-auto space-y-4 bg-gray-50/50">
            <div class="flex items-start space-x-2">
                <div class="w-8 h-8 rounded-full bg-indigo-100 text-indigo-600 flex items-center justify-center flex-shrink-0 text-sm font-bold">AI</div>
                <div class="bg-white p-3 rounded-2xl rounded-tl-none shadow-sm border border-gray-100 text-xs text-gray-700 max-w-[75%] leading-relaxed">
                    Chào bạn! Tôi là trợ lý gợi ý sản phẩm thông minh. Bạn muốn tìm trang phục nào hôm nay? (Ví dụ: "tìm áo nam dưới 500k", "váy nữ màu đỏ",...)
                </div>
            </div>
        </div>
        
        <!-- Input Area -->
        <div class="p-3 bg-white border-t border-gray-100 flex items-center space-x-2">
            <input type="text" id="chat-input" placeholder="Nhập tin nhắn tìm đồ..." class="flex-1 px-4 py-2 border border-gray-200 rounded-full text-xs focus:outline-none focus:ring-2 focus:ring-indigo-500 bg-gray-50/50 transition">
            <button id="chat-send" class="bg-indigo-600 hover:bg-indigo-700 text-white rounded-full p-2.5 flex items-center justify-center transition shadow-md shadow-indigo-100">
                <i class="fa-solid fa-paper-plane text-sm"></i>
            </button>
        </div>
    </div>
    `;
    
    const wrapper = document.createElement("div");
    wrapper.innerHTML = chatboxHtml;
    document.body.appendChild(wrapper);
    
    // Toggle Events
    const toggleBtn = document.getElementById("chat-toggle");
    const closeBtn = document.getElementById("chat-close");
    const chatWindow = document.getElementById("chat-window");
    const chatInput = document.getElementById("chat-input");
    const sendBtn = document.getElementById("chat-send");
    const messagesBox = document.getElementById("chat-messages");
    
    toggleBtn.addEventListener("click", () => {
        chatWindow.classList.toggle("opacity-0");
        chatWindow.classList.toggle("scale-95");
        chatWindow.classList.toggle("pointer-events-none");
        chatInput.focus();
    });
    
    closeBtn.addEventListener("click", () => {
        chatWindow.classList.add("opacity-0");
        chatWindow.classList.add("scale-95");
        chatWindow.classList.add("pointer-events-none");
    });
    
    // SignalR Hub Connection Setup
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(`${API_BASE_URL}/chatHub`, {
            skipNegotiation: true,
            transport: signalR.HttpTransportType.WebSockets
        })
        .withAutomaticReconnect()
        .build();

    connection.on("ReceiveMessage", (responseText, suggestedProducts) => {
        // Append response text
        appendMessage("AI", responseText);
        
        // Append product cards if suggested
        if (suggestedProducts && suggestedProducts.length > 0) {
            appendProductSuggestions(suggestedProducts);
        }
    });

    connection.start().then(() => {
        console.log("SignalR Connection established successfully.");
    }).catch(err => {
        console.error("SignalR connection failed, falling back to REST API:", err);
    });
    
    // Sending message
    async function sendMessage() {
        const text = chatInput.value.trim();
        if (!text) return;
        
        // Append user message
        appendMessage("You", text);
        chatInput.value = "";
        
        // Try SignalR first, fallback to REST API
        if (connection.state === signalR.HubConnectionState.Connected) {
            try {
                await connection.invoke("SendMessage", text);
            } catch (err) {
                console.warn("SignalR Send failed, trying REST API:", err);
                await fallbackRestChat(text);
            }
        } else {
            await fallbackRestChat(text);
        }
    }
    
    async function fallbackRestChat(text) {
        try {
            // Append loading indicator
            const loadId = appendLoading();
            
            // Call simulated API (Or ChatApi fallback endpoint if implemented)
            const res = await apiFetch("ChatApi", {
                method: "POST",
                body: { message: text }
            });
            
            removeLoading(loadId);
            
            if (res.ok) {
                const data = await res.json();
                appendMessage("AI", data.responseText);
                if (data.suggestedProducts && data.suggestedProducts.length > 0) {
                    appendProductSuggestions(data.suggestedProducts);
                }
            } else {
                appendMessage("AI", "Xin lỗi, hiện trợ lý ảo đang bận, vui lòng thử lại sau.");
            }
        } catch (e) {
            appendMessage("AI", "Xin lỗi, đã xảy ra lỗi kết nối với trợ lý gợi ý.");
        }
    }
    
    sendBtn.addEventListener("click", sendMessage);
    chatInput.addEventListener("keydown", (e) => {
        if (e.key === "Enter") sendMessage();
    });
    
    function appendMessage(sender, text) {
        const isUser = sender === "You";
        const messageDiv = document.createElement("div");
        messageDiv.className = `flex items-start space-x-2 ${isUser ? 'justify-end' : ''}`;
        
        const avatar = isUser ? '' : `<div class="w-8 h-8 rounded-full bg-indigo-100 text-indigo-600 flex items-center justify-center flex-shrink-0 text-sm font-bold">AI</div>`;
        const bgClass = isUser ? 'bg-indigo-600 text-white rounded-tr-none' : 'bg-white text-gray-700 rounded-tl-none border border-gray-100';
        
        messageDiv.innerHTML = `
            ${avatar}
            <div class="${bgClass} p-3 rounded-2xl shadow-sm text-xs max-w-[75%] leading-relaxed">
                ${text}
            </div>
        `;
        messagesBox.appendChild(messageDiv);
        messagesBox.scrollTop = messagesBox.scrollHeight;
    }
    
    function appendProductSuggestions(products) {
        const containerDiv = document.createElement("div");
        containerDiv.className = "pl-10 space-y-2";
        
        const scrollDiv = document.createElement("div");
        scrollDiv.className = "flex space-x-3 overflow-x-auto pb-2 scrollbar-thin";
        
        products.forEach(p => {
            const card = document.createElement("div");
            card.className = "bg-white rounded-xl border border-gray-100 shadow-sm p-2 w-44 flex-shrink-0 hover:border-indigo-200 transition";
            
            const priceHtml = p.discountPrice 
                ? `<div class="flex items-center space-x-1.5"><span class="text-xs font-bold text-rose-500">${p.discountPrice.toLocaleString("vi-VN")}đ</span><span class="text-[9px] line-through text-gray-400">${p.price.toLocaleString("vi-VN")}đ</span></div>` 
                : `<span class="text-xs font-bold text-gray-900">${p.price.toLocaleString("vi-VN")}đ</span>`;
            
            card.innerHTML = `
                <img src="${p.imageUrl}" alt="${p.name}" class="w-full h-24 object-cover rounded-lg mb-2">
                <h4 class="text-[10px] font-bold text-gray-800 line-clamp-2 min-h-[30px]">${p.name}</h4>
                <div class="mt-1 flex justify-between items-center">
                    ${priceHtml}
                </div>
                <a href="detail.html?id=${p.id}" class="mt-2 block w-full text-center py-1 bg-gray-50 hover:bg-indigo-50 hover:text-indigo-600 text-[10px] font-semibold text-gray-600 rounded-lg transition border border-gray-100">Chi tiết</a>
            `;
            scrollDiv.appendChild(card);
        });
        
        containerDiv.appendChild(scrollDiv);
        messagesBox.appendChild(containerDiv);
        messagesBox.scrollTop = messagesBox.scrollHeight;
    }
    
    let loadCounter = 0;
    function appendLoading() {
        loadCounter++;
        const id = `chat-loading-${loadCounter}`;
        const loadDiv = document.createElement("div");
        loadDiv.id = id;
        loadDiv.className = "flex items-start space-x-2";
        loadDiv.innerHTML = `
            <div class="w-8 h-8 rounded-full bg-indigo-100 text-indigo-600 flex items-center justify-center flex-shrink-0 text-sm font-bold">AI</div>
            <div class="bg-white p-3 rounded-2xl rounded-tl-none border border-gray-100 shadow-sm text-xs text-gray-400 flex items-center space-x-1">
                <div class="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce"></div>
                <div class="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 0.2s"></div>
                <div class="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 0.4s"></div>
            </div>
        `;
        messagesBox.appendChild(loadDiv);
        messagesBox.scrollTop = messagesBox.scrollHeight;
        return id;
    }
    
    function removeLoading(id) {
        const el = document.getElementById(id);
        if (el) el.remove();
    }
}

// Global Startup Initializations
document.addEventListener("DOMContentLoaded", async () => {
    // Determine active login state
    await checkAuthStatus();
    
    // Inject components
    injectHeader();
    injectFooter();
    // Khởi động typewriter logo
    startLogoTypewriter();
    
    // Load SignalR library and inject chatbox
    const script = document.createElement("script");
    script.src = "https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js";
    script.onload = () => {
        injectChatbox();
    };
    document.head.appendChild(script);
    
    // Sync cart count
    updateCartCountBadge();
});
