const API_URL = 'http://localhost:5026/api';

let currentUser = null;

// Проверка авторизации
function checkAuth() {
    const token = localStorage.getItem('token');
    if (!token && window.location.pathname.includes('login.html')) {
        return;
    }
    
    if (!token && !window.location.pathname.includes('login.html')) {
        window.location.href = 'login.html';
        return;
    }
    
    if (token && window.location.pathname.includes('login.html')) {
        window.location.href = 'index.html';
        return;
    }
    
    if (token) {
        loadUserInfo();
    }
}

// Загрузка информации о пользователе
async function loadUserInfo() {
    try {
        const token = localStorage.getItem('token');
        if (!token) return;
        
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        
        const payload = JSON.parse(jsonPayload);
        
        currentUser = {
            id: payload.nameid,
            email: payload.email,
            role: payload.role,
            fullName: payload.FullName
        };
        
        if (document.getElementById('user-name')) {
            document.getElementById('user-name').textContent = currentUser.fullName;
            document.getElementById('user-role').textContent = currentUser.role === 'Admin' ? 'Администратор' : 'Исполнитель';
        }
        
        if (currentUser.role !== 'Admin') {
            const adminElements = document.querySelectorAll('.admin-only');
            adminElements.forEach(el => el.style.display = 'none');
        }
    } catch (error) {
        console.error('Ошибка загрузки информации пользователя:', error);
        localStorage.removeItem('token');
        window.location.href = 'login.html';
    }
}

// Выполнение API запроса
async function apiRequest(endpoint, method = 'GET', data = null) {
    const token = localStorage.getItem('token');
    const headers = {
        'Content-Type': 'application/json'
    };
    
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }
    
    const options = {
        method,
        headers
    };
    
    if (data) {
        options.body = JSON.stringify(data);
    }
    
    try {
        const response = await fetch(`${API_URL}${endpoint}`, options);
        
        if (response.status === 401) {
            localStorage.removeItem('token');
            window.location.href = 'login.html';
            return null;
        }
        
        const text = await response.text();
        let result;
        
        try {
            result = JSON.parse(text);
        } catch (e) {
            console.error('Ошибка парсинга JSON:', text);
            throw new Error('Сервер вернул ошибку');
        }
        
        if (!response.ok) {
            throw new Error(result.message || result.error || 'Ошибка запроса');
        }
        
        return result;
    } catch (error) {
        console.error('API Error:', error);
        showError(error.message);
        return null;
    }
}

function showError(message) {
    alert(message);
}

function logout() {
    localStorage.removeItem('token');
    window.location.href = 'login.html';
}

async function loadDashboardStats() {
    try {
        const stats = await apiRequest('/dashboard/stats');
        if (stats) {
            document.getElementById('orders-month').textContent = stats.ordersThisMonth || 0;
            document.getElementById('income-month').textContent = `${(stats.incomeThisMonth || 0).toLocaleString()} ₽`;
            document.getElementById('orders-progress').textContent = stats.ordersInProgress || 0;
            document.getElementById('average-check').textContent = `${Math.round(stats.averageCheck || 0).toLocaleString()} ₽`;
            
            if (stats.topServices && stats.topServices.length > 0) {
                const topServicesHtml = stats.topServices.map(service => `
                    <tr>
                        <td>${service.serviceName || '—'}</td>
                        <td>${(service.totalIncome || 0).toLocaleString()} ₽</td>
                        <td>${service.ordersCount || 0}</td>
                    </tr>
                `).join('');
                document.getElementById('top-services').innerHTML = topServicesHtml;
            } else {
                document.getElementById('top-services').innerHTML = '<tr><td colspan="3">Нет данных</td></tr>';
            }
        }
    } catch (error) {
        console.error('Ошибка загрузки статистики:', error);
        document.getElementById('orders-month').textContent = '0';
        document.getElementById('income-month').textContent = '0 ₽';
        document.getElementById('orders-progress').textContent = '0';
        document.getElementById('average-check').textContent = '0 ₽';
    }
}

async function loadOrders(statusFilter = '') {
    const orders = await apiRequest('/orders');
    if (orders) {
        let filteredOrders = orders;
        if (statusFilter) {
            filteredOrders = orders.filter(o => o.status === statusFilter);
        }
        
        const ordersHtml = filteredOrders.map(order => `
            <tr>
                <td>${order.id}</td>
                <td>${order.clientName}</td>
                <td>${order.serviceName}</td>
                <td>${order.price.toLocaleString()} ₽</td>
                <td>
                    <select class="status-badge status-${order.status}" onchange="updateOrderStatus(${order.id}, this.value)">
                        <option value="New" ${order.status === 'New' ? 'selected' : ''}>Новый</option>
                        <option value="InProgress" ${order.status === 'InProgress' ? 'selected' : ''}>В работе</option>
                        <option value="Completed" ${order.status === 'Completed' ? 'selected' : ''}>Выполнен</option>
                        <option value="Paid" ${order.status === 'Paid' ? 'selected' : ''}>Оплачен</option>
                    </select>
                   </td>
                <td>${order.executorName}</td>
                <td>${new Date(order.createdAt).toLocaleDateString()}</td>
                <td>
                    ${currentUser && currentUser.role === 'Admin' ? `<button class="btn-small" onclick="deleteOrder(${order.id})">Удалить</button>` : ''}
                   </td>
               </tr>
        `).join('');
        
        document.getElementById('orders-table-body').innerHTML = ordersHtml || '<tr><td colspan="8">Нет заказов</td></tr>';
    }
}

async function updateOrderStatus(orderId, status) {
    const result = await apiRequest(`/orders/${orderId}/status`, 'PUT', status);
    if (result) {
        loadOrders(document.getElementById('status-filter')?.value || '');
    }
}

async function deleteOrder(orderId) {
    if (confirm('Вы уверены, что хотите удалить этот заказ?')) {
        const result = await apiRequest(`/orders/${orderId}`, 'DELETE');
        if (result) {
            loadOrders(document.getElementById('status-filter')?.value || '');
            loadDashboardStats();
        }
    }
}

async function loadClients() {
    const clients = await apiRequest('/clients');
    if (clients) {
        const clientsHtml = clients.map(client => `
            <tr>
                <td>${client.name}</td>
                <td>${client.phone}</td>
                <td>${client.email}</td>
                <td>${client.ordersCount}</td>
                <td>${(client.totalSum || 0).toLocaleString()} ₽</td>
                <td>
                    ${currentUser && currentUser.role === 'Admin' ? `
                        <button class="btn-small" onclick="editClient(${client.id})">Редактировать</button>
                        <button class="btn-small btn-danger" onclick="deleteClient(${client.id})">Удалить</button>
                    ` : ''}
                   </td>
               </tr>
        `).join('');
        
        document.getElementById('clients-table-body').innerHTML = clientsHtml || '<tr><td colspan="6">Нет клиентов</td></tr>';
    }
}

async function deleteClient(clientId) {
    if (confirm('Вы уверены, что хотите удалить этого клиента?')) {
        const result = await apiRequest(`/clients/${clientId}`, 'DELETE');
        if (result) {
            loadClients();
        }
    }
}

// Редактирование клиента - открывает форму с заполненными данными
async function editClient(clientId) {
    try {
        const response = await fetch(`${API_URL}/clients/${clientId}`, {
            headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
        });
        
        if (!response.ok) throw new Error('Ошибка загрузки');
        
        const client = await response.json();
        
        if (client) {
            document.getElementById('client-name').value = client.name;
            document.getElementById('client-phone').value = client.phone || '';
            document.getElementById('client-email').value = client.email || '';
            
            document.querySelector('#client-modal h3').textContent = '✏️ Редактирование клиента';
            
            const submitBtn = document.querySelector('#client-form button[type="submit"]');
            if (submitBtn) {
                submitBtn.textContent = '💾 Сохранить изменения';
            }
            
            window.editingClientId = clientId;
            openCreateClientModal();
        }
    } catch (error) {
        console.error('Ошибка загрузки клиента:', error);
        alert('Не удалось загрузить данные клиента');
    }
}

// Обновить клиента
async function updateClient(clientId) {
    const name = document.getElementById('client-name').value;
    const phone = document.getElementById('client-phone').value;
    const email = document.getElementById('client-email').value;
    
    if (!name) {
        alert('Введите название компании или ФИО');
        return;
    }
    
    const clientData = {
        id: clientId,
        name: name,
        phone: phone || '',
        email: email || ''
    };
    
    try {
        const result = await apiRequest(`/clients/${clientId}`, 'PUT', clientData);
        if (result) {
            alert('✅ Клиент успешно обновлён!');
            closeClientModal();
            loadClients();
            resetClientForm();
        }
    } catch (error) {
        console.error('Ошибка:', error);
        alert('Ошибка при обновлении клиента: ' + error.message);
    }
}

// Сброс формы клиента
function resetClientForm() {
    document.getElementById('client-form').reset();
    document.querySelector('#client-modal h3').textContent = '➕ Добавление нового клиента';
    const submitBtn = document.querySelector('#client-form button[type="submit"]');
    if (submitBtn) {
        submitBtn.textContent = '💾 Сохранить клиента';
    }
    delete window.editingClientId;
}

function openCreateOrderModal() {
    document.getElementById('order-modal').style.display = 'flex';
    document.getElementById('order-form').reset();
}

function closeModal() {
    document.getElementById('order-modal').style.display = 'none';
}

async function saveOrder() {
    const clientId = parseInt(document.getElementById('client-id').value);
    const serviceName = document.getElementById('service-name').value;
    const price = parseFloat(document.getElementById('price').value);
    const executorId = document.getElementById('executor-id').value;
    
    if (!clientId || clientId <= 0) {
        alert('Введите ID клиента (1, 2 или 3)');
        return;
    }
    
    if (!serviceName) {
        alert('Введите название услуги');
        return;
    }
    
    if (!price || price <= 0) {
        alert('Введите корректную стоимость');
        return;
    }
    
    const orderData = {
        clientId: clientId,
        serviceName: serviceName,
        price: price,
        executorId: executorId ? parseInt(executorId) : null
    };
    
    console.log('Отправляем заказ:', orderData);
    
    try {
        const result = await apiRequest('/orders', 'POST', orderData);
        if (result) {
            alert('✅ Заказ успешно создан!');
            closeModal();
            loadOrders();
            loadDashboardStats();
            document.getElementById('order-form').reset();
        }
    } catch (error) {
        console.error('Ошибка:', error);
        alert('Ошибка при создании заказа: ' + error.message);
    }
}

// Открыть модальное окно создания клиента
function openCreateClientModal() {
    document.getElementById('client-modal').style.display = 'flex';
}

// Закрыть модальное окно клиента
function closeClientModal() {
    document.getElementById('client-modal').style.display = 'none';
    resetClientForm();
}

// Сохранить нового клиента
async function saveClient() {
    const name = document.getElementById('client-name').value;
    const phone = document.getElementById('client-phone').value;
    const email = document.getElementById('client-email').value;
    
    if (!name) {
        alert('Введите название компании или ФИО');
        return;
    }
    
    const clientData = {
        name: name,
        phone: phone || '',
        email: email || ''
    };
    
    console.log('Отправляем клиента:', clientData);
    
    try {
        const result = await apiRequest('/clients', 'POST', clientData);
        if (result) {
            alert('✅ Клиент успешно добавлен!');
            closeClientModal();
            loadClients();
        }
    } catch (error) {
        console.error('Ошибка:', error);
        alert('Ошибка при добавлении клиента: ' + error.message);
    }
}

// Загрузка аналитики
async function loadAnalytics() {
    const orders = await apiRequest('/orders');
    if (!orders) return;
    
    const clientTotals = {};
    orders.forEach(order => {
        if (order.status === 'Paid') {
            clientTotals[order.clientName] = (clientTotals[order.clientName] || 0) + order.price;
        }
    });
    const topClient = Object.entries(clientTotals).sort((a,b) => b[1] - a[1])[0];
    if (document.getElementById('top-client')) {
        document.getElementById('top-client').textContent = topClient ? `${topClient[0]} (${topClient[1].toLocaleString()} ₽)` : '—';
    }
    
    const serviceCount = {};
    orders.forEach(order => {
        serviceCount[order.serviceName] = (serviceCount[order.serviceName] || 0) + 1;
    });
    const topService = Object.entries(serviceCount).sort((a,b) => b[1] - a[1])[0];
    if (document.getElementById('top-service')) {
        document.getElementById('top-service').textContent = topService ? `${topService[0]} (${topService[1]} заказов)` : '—';
    }
    
    const total = orders.length;
    const paid = orders.filter(o => o.status === 'Paid').length;
    const conversion = total > 0 ? ((paid / total) * 100).toFixed(1) : 0;
    if (document.getElementById('conversion-rate')) {
        document.getElementById('conversion-rate').textContent = `${conversion}%`;
    }
}

// Показ страницы
function showPage(pageId) {
    const pages = ['stats-page', 'orders-page', 'clients-page', 'analytics-page'];
    pages.forEach(page => {
        const element = document.getElementById(page);
        if (element) element.style.display = 'none';
    });
    const selectedPage = document.getElementById(pageId);
    if (selectedPage) selectedPage.style.display = 'block';
    
    if (pageId === 'stats-page') loadDashboardStats();
    if (pageId === 'orders-page') loadOrders();
    if (pageId === 'clients-page') loadClients();
    if (pageId === 'analytics-page') loadAnalytics();
}

// Инициализация
document.addEventListener('DOMContentLoaded', () => {
    checkAuth();
    
    if (document.querySelector('.nav-item')) {
        document.querySelectorAll('.nav-item').forEach(item => {
            item.addEventListener('click', function(e) {
                const onclickAttr = this.getAttribute('onclick');
                if (onclickAttr) {
                    const match = onclickAttr.match(/'([^']+)'/);
                    if (match) {
                        const pageId = match[1];
                        showPage(pageId);
                        document.querySelectorAll('.nav-item').forEach(nav => nav.classList.remove('active'));
                        this.classList.add('active');
                    }
                }
            });
        });
    }
    
    if (document.getElementById('logout-btn')) {
        document.getElementById('logout-btn').addEventListener('click', logout);
    }
    
    if (document.getElementById('order-form')) {
        document.getElementById('order-form').addEventListener('submit', (e) => {
            e.preventDefault();
            saveOrder();
        });
    }
    
    if (document.getElementById('status-filter')) {
        document.getElementById('status-filter').addEventListener('change', (e) => {
            loadOrders(e.target.value);
        });
    }

    if (document.getElementById('client-form')) {
        document.getElementById('client-form').addEventListener('submit', (e) => {
            e.preventDefault();
            if (window.editingClientId) {
                updateClient(window.editingClientId);
            } else {
                saveClient();
            }
        });
    }

    window.onclick = function(event) {
        const orderModal = document.getElementById('order-modal');
        const clientModal = document.getElementById('client-modal');
        if (event.target === orderModal) {
            closeModal();
        }
        if (event.target === clientModal) {
            closeClientModal();
        }
    }
});