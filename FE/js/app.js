// Configuration
const API_BASE_URL = "https://localhost:7057";

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
            // Handle unauthorized - clear local state and redirect to login if not already on login
            localStorage.removeItem("user");
            if (!window.location.pathname.endsWith("login.html") && !window.location.pathname.endsWith("register.html") && !window.location.pathname.endsWith("index.html") && !window.location.pathname.endsWith("shop.html") && !window.location.pathname.endsWith("detail.html") && !window.location.pathname.endsWith("cart.html")) {
                window.location.href = "login.html";
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

// Cart Management (LocalStorage for Guest, API for User)
const LOCAL_CART_KEY = "GuestCart";

function getLocalCart() {
    const data = localStorage.getItem(LOCAL_CART_KEY);
    return data ? JSON.parse(data) : [];
}

function saveLocalCart(cart) {
    localStorage.setItem(LOCAL_CART_KEY, JSON.stringify(cart));
    updateCartCountBadge();
}

async function addToCart(variantId, quantity = 1) {
    const user = getCurrentUser();
    if (user) {
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
    } else {
        // Guest mode
        const cart = getLocalCart();
        const existing = cart.find(i => i.productVariantId === variantId);
        if (existing) {
            existing.quantity += quantity;
        } else {
            cart.push({ productVariantId: variantId, quantity });
        }
        saveLocalCart(cart);
        return { success: true, message: "Đã thêm sản phẩm vào giỏ hàng thành công! (Lưu tạm thời)" };
    }
}

async function getCartItems() {
    const user = getCurrentUser();
    if (user) {
        try {
            const res = await apiFetch("CartApi");
            if (res.ok) {
                return await res.json();
            }
        } catch (e) {
            console.error("Error loading cart:", e);
        }
        return [];
    } else {
        // Guest Mode: Resolve details from Backend
        const localCart = getLocalCart();
        if (localCart.length === 0) return [];
        
        try {
            // Retrieve all products to resolve details
            const res = await apiFetch("ProductApi?page=1&pageSize=100");
            if (res.ok) {
                const data = await res.json();
                const resolved = [];
                for (const item of localCart) {
                    // Search variants
                    for (const prod of data.products) {
                        // Fetch detail for this product to get full variant list
                        const detailRes = await apiFetch(`ProductApi/${prod.id}`);
                        if (detailRes.ok) {
                            const detail = await detailRes.json();
                            const variant = detail.variants.find(v => v.id === item.productVariantId);
                            if (variant) {
                                resolved.push({
                                    id: 0, // Guest cart item has 0 DB id
                                    productVariantId: variant.id,
                                    productName: detail.name,
                                    size: variant.size,
                                    color: variant.color,
                                    price: detail.discountPrice || detail.price,
                                    imageUrl: detail.images.find(img => img.isMain)?.imageUrl || "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?w=400",
                                    quantity: item.quantity,
                                    stockQuantity: variant.stock,
                                    totalPrice: (detail.discountPrice || detail.price) * item.quantity
                                });
                                break;
                            }
                        }
                    }
                }
                return resolved;
            }
        } catch (e) {
            console.error("Error fetching guest details:", e);
        }
        return [];
    }
}

async function updateCartQuantity(cartItemId, variantId, quantity) {
    const user = getCurrentUser();
    if (user) {
        const res = await apiFetch("CartApi/update", {
            method: "POST",
            body: { cartItemId, variantId, quantity }
        });
        const data = await res.json();
        return { success: res.ok, message: data.message };
    } else {
        const cart = getLocalCart();
        const item = cart.find(i => i.productVariantId === variantId);
        if (item) {
            item.quantity = quantity;
            saveLocalCart(cart);
            return { success: true, message: "Cập nhật số lượng thành công!" };
        }
        return { success: false, message: "Không tìm thấy sản phẩm." };
    }
}

async function removeCartItem(cartItemId, variantId) {
    const user = getCurrentUser();
    if (user) {
        const res = await apiFetch("CartApi/remove", {
            method: "POST",
            body: { cartItemId }
        });
        const data = await res.json();
        return { success: res.ok, message: data.message };
    } else {
        let cart = getLocalCart();
        cart = cart.filter(i => i.productVariantId !== variantId);
        saveLocalCart(cart);
        return { success: true, message: "Đã xóa sản phẩm khỏi giỏ hàng!" };
    }
}

async function syncGuestCartOnLogin() {
    const localCart = getLocalCart();
    if (localCart.length > 0) {
        try {
            const formatted = localCart.map(i => ({ productVariantId: i.productVariantId, quantity: i.quantity }));
            const res = await apiFetch("CartApi/sync", {
                method: "POST",
                body: formatted
            });
            if (res.ok) {
                localStorage.removeItem(LOCAL_CART_KEY);
            }
        } catch (e) {
            console.warn("Cart synchronization failed.");
        }
    }
}

async function updateCartCountBadge() {
    const badge = document.getElementById("cart-badge");
    if (!badge) return;
    
    const user = getCurrentUser();
    let count = 0;
    if (user) {
        try {
            const res = await apiFetch("CartApi");
            if (res.ok) {
                const items = await res.json();
                count = items.reduce((sum, item) => sum + item.quantity, 0);
            }
        } catch (e) {
            console.warn("Could not retrieve cart count.");
        }
    } else {
        const cart = getLocalCart();
        count = cart.reduce((sum, item) => sum + item.quantity, 0);
    }
    
    badge.innerText = count;
    badge.style.display = count > 0 ? "flex" : "none";
}

// Global UI Rendering Helpers
function injectHeader() {
    const headerHtml = `
    <header class="sticky top-0 z-50 bg-white/80 backdrop-blur-md border-b border-gray-100 transition-all duration-300">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div class="flex justify-between items-center h-16">
                <!-- Logo -->
                <div class="flex-shrink-0">
                    <a href="index.html" class="flex items-center space-x-2">
                        <span class="text-2xl font-black tracking-wider bg-gradient-to-r from-indigo-600 to-violet-600 bg-clip-text text-transparent">CLOTHINGSHOP</span>
                    </a>
                </div>
                
                <!-- Nav Links -->
                <nav class="hidden md:flex space-x-8">
                    <a href="index.html" class="text-gray-600 hover:text-indigo-600 px-3 py-2 text-sm font-medium transition">Trang Chủ</a>
                    <a href="shop.html" class="text-gray-600 hover:text-indigo-600 px-3 py-2 text-sm font-medium transition">Cửa Hàng</a>
                </nav>
                
                <!-- Actions -->
                <div class="flex items-center space-x-4">
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
    
    const wrapper = document.createElement("div");
    wrapper.innerHTML = headerHtml;
    document.body.prepend(wrapper.firstElementChild);
    
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
                        <a href="profile.html" class="flex items-center px-4 py-2.5 text-sm text-gray-700 hover:bg-gray-50"><i class="fa-regular fa-id-card mr-2 text-gray-400"></i>Hồ sơ của tôi</a>
                        <a href="profile.html#orders" class="flex items-center px-4 py-2.5 text-sm text-gray-700 hover:bg-gray-50"><i class="fa-solid fa-receipt mr-2 text-gray-400"></i>Lịch sử đơn hàng</a>
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
            <div class="grid grid-cols-1 md:grid-cols-4 gap-8 mb-12">
                <div class="md:col-span-2">
                    <span class="text-2xl font-black tracking-wider text-white bg-gradient-to-r from-indigo-400 to-violet-400 bg-clip-text text-transparent">CLOTHINGSHOP</span>
                    <p class="mt-4 text-sm leading-relaxed max-w-sm text-gray-400">Website mua sắm thời trang trực tuyến hàng đầu Việt Nam. Cung cấp các sản phẩm chất lượng cao với xu hướng thiết kế mới nhất.</p>
                </div>
                <div>
                    <h3 class="text-sm font-semibold text-white uppercase tracking-wider mb-4">Danh mục</h3>
                    <ul class="space-y-2">
                        <li><a href="shop.html?categoryId=1" class="hover:text-white transition">Áo Nam</a></li>
                        <li><a href="shop.html?categoryId=2" class="hover:text-white transition">Quần Nam</a></li>
                        <li><a href="shop.html?categoryId=3" class="hover:text-white transition">Áo Nữ</a></li>
                        <li><a href="shop.html?categoryId=4" class="hover:text-white transition">Váy Nữ</a></li>
                    </ul>
                </div>
                <div>
                    <h3 class="text-sm font-semibold text-white uppercase tracking-wider mb-4">Hỗ trợ</h3>
                    <p class="text-sm">Hotline: 1800 6688</p>
                    <p class="text-sm mt-2">Email: support@clothingshop.com</p>
                    <p class="text-sm mt-2">Địa chỉ: 123 Main St, Hanoi, Vietnam</p>
                </div>
            </div>
            <div class="border-t border-gray-800 pt-8 flex flex-col md:flex-row justify-between items-center text-xs">
                <p>&copy; 2026 ClothingShop. All Rights Reserved.</p>
                <div class="flex space-x-6 mt-4 md:mt-0">
                    <a href="#" class="hover:text-white"><i class="fa-brands fa-facebook text-lg"></i></a>
                    <a href="#" class="hover:text-white"><i class="fa-brands fa-instagram text-lg"></i></a>
                    <a href="#" class="hover:text-white"><i class="fa-brands fa-youtube text-lg"></i></a>
                </div>
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
                ? `<div class="flex items-center space-x-1.5"><span class="text-xs font-bold text-rose-500">${p.discountPrice.toLocaleString("N0")}đ</span><span class="text-[9px] line-through text-gray-400">${p.price.toLocaleString("N0")}đ</span></div>` 
                : `<span class="text-xs font-bold text-gray-900">${p.price.toLocaleString("N0")}đ</span>`;
            
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
