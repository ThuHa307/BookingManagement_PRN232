// Khởi tạo các biến trạng thái
let currentConversationId = null;
let currentReceiverId = null;
let connection = null;

$(document).ready(function () {
    $('#sendBtn').prop('disabled', true); // Disable nút gửi khi chưa kết nối
    loadConversations();
    loadQuickReplies();
    initSignalR();

    // Sự kiện gửi tin nhắn
    $('#sendBtn').on('click', sendMessageHandler);
    $('#messageInput').on('keypress', function (e) {
        if (e.key === 'Enter') sendMessageHandler();
    });

    // Sự kiện chọn hội thoại
    $(document).on('click', '.conversation-item', function () {
        $(this).removeClass('bg-light');
        $(this).find('.last-message').remove();
        currentConversationId = $(this).data('id');
        currentReceiverId = $(this).data('receiver-id');
        loadConversationDetail(currentConversationId);
    });

    // Sự kiện thêm quick reply (hiện popup nhập nội dung và gọi API)
    $('#openQuickMsgPopup').on('click', function () {
        Swal.fire({
            title: 'Thêm tin nhắn nhanh',
            input: 'text',
            inputLabel: 'Nội dung tin nhắn',
            inputPlaceholder: 'Nhập nội dung...',
            showCancelButton: true,
            confirmButtonText: 'Lưu',
            cancelButtonText: 'Hủy',
            inputValidator: (value) => {
                if (!value || value.trim().length === 0) {
                    return 'Vui lòng nhập nội dung!';
                }
            }
        }).then((result) => {
            if (result.isConfirmed && result.value && result.value.trim().length > 0) {
                $.ajax({
                    url: 'https://localhost:7225/api/chatroom/quick-message',
                    method: 'POST',
                    contentType: 'application/json',
                    headers: {
                        'Authorization': 'Bearer ' + getAccessToken()
                    },
                    data: JSON.stringify({ content: result.value.trim() }),
                    success: function (res) {
                        Swal.fire('Thành công', 'Đã thêm tin nhắn nhanh!', 'success');
                        loadQuickReplies();
                    },
                    error: function (xhr) {
                        let msg = 'Đã có lỗi xảy ra!';
                        if (xhr.responseJSON && xhr.responseJSON.message) msg = xhr.responseJSON.message;
                        Swal.fire('Lỗi', msg, 'error');
                    }
                });
            }
        });
    });
});

function getAccessToken() {
    // Ưu tiên lấy từ localStorage, nếu không có thì thử từ sessionStorage hoặc cookie
    return localStorage.getItem('accessToken') || sessionStorage.getItem('accessToken') || getCookie('accessToken');
}
function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
}

function loadConversations() {
    $.ajax({
        url: 'https://localhost:7225/api/chatroom/conversations',
        method: 'GET',
        headers: {
            'Authorization': 'Bearer ' + getAccessToken()
        },
        success: function (data) {
            let html = '';
            if (!data || data.length === 0) {
                html = `<div class="text-center text-muted p-4">
                            <i class="fas fa-comments-slash fa-2x mb-2"></i><br />
                            Bạn hiện chưa có cuộc trò chuyện nào.
                        </div>`;
            } else {
                data.forEach(function (conversation) {
                    // Xác định user còn lại (không phải mình)
                    let currentUserId = conversation.senderId; // Giả định sender là mình, sẽ sửa lại nếu có userId
                    let otherUser = conversation.receiver;
                    if (window.currentUserId && conversation.senderId === window.currentUserId) {
                        otherUser = conversation.receiver;
                    } else if (window.currentUserId && conversation.receiverId === window.currentUserId) {
                        otherUser = conversation.sender;
                    }
                    let avatar = otherUser?.avatarUrl || '/images/person_1.jpg';
                    let fullName = (otherUser?.firstName || '') + ' ' + (otherUser?.lastName || '');
                    let postTitle = conversation.post?.title || '';
                    html += `<a class="list-group-item list-group-item-action d-flex align-items-center conversation-item"
                                data-id="${conversation.conversationId}" data-receiver-id="${otherUser.accountId}">
                                <img src="${avatar}" class="rounded-circle mr-3" width="40" height="40" />
                                <div class="ml-2">
                                    <div class="font-weight-bold">${fullName}</div>
                                    <small>${postTitle}</small>
                                </div>
                            </a>`;
                });
            }
            $('#conversation-list').html(html);
            // Tự động chọn hội thoại đầu tiên nếu chưa chọn
            if (data && data.length > 0 && !currentConversationId) {
                const first = data[0];
                currentConversationId = first.conversationId;
                currentReceiverId = (window.currentUserId && first.senderId === window.currentUserId) ? first.receiver.accountId : first.sender.accountId;
                loadConversationDetail(currentConversationId);
            }
        },
        error: function(xhr) {
            if (xhr.status === 401) {
                Swal.fire('Bạn cần đăng nhập để sử dụng chat room!');
            }
        }
    });
}

function loadConversationDetail(conversationId) {
    $.ajax({
        url: `https://localhost:7225/api/chatroom/detail/${conversationId}`,
        method: 'GET',
        headers: {
            'Authorization': 'Bearer ' + getAccessToken()
        },
        success: function (data) {
            // Render thông tin người nhận
            let otherUser = (window.currentUserId && data.senderId === window.currentUserId) ? data.receiver : data.sender;
            let avatar = otherUser.avatarUrl || '/images/person_1.jpg';
            let fullName = (otherUser.firstName || '') + ' ' + (otherUser.lastName || '');
            let isOnline = otherUser.isOnline ? 'Đang hoạt động' : 'Ngoại tuyến';
            let statusDotClass = otherUser.isOnline ? 'status-online' : 'status-offline';
            $('#receiverInfo').html(`
                <div class="d-flex align-items-center">
                    <img src="${avatar}" class="rounded-circle mr-3" width="40" height="40" />
                    <div>
                        <div class="font-weight-bold">${fullName}</div>
                        <div class="d-flex align-items-center my-2">
                            <span class="status-dot ${statusDotClass}"></span>
                            <small class="ml-2" style="line-height: 0 !important;">${isOnline}</small>
                        </div>
                    </div>
                </div>
            `);
            // Render bài đăng liên quan
            if (data.post) {
                $('#postInfo').removeClass('d-none').html(`
                    <div class="d-flex align-items-center">
                        <img src="${data.post.imageUrl || '/images/work-1.jpg'}" class="mr-3" width="60" height="60" style="object-fit: cover;" />
                        <div>
                            <div class="font-weight-bold">${data.post.title || ''}</div>
                            <div class="text-muted">${data.post.price ? data.post.price.toLocaleString() + ' VNĐ/tháng' : ''}</div>
                        </div>
                    </div>
                `);
            } else {
                $('#postInfo').addClass('d-none').html('');
            }
            // Render tin nhắn
            let messagesHtml = '';
            let lastDateStr = '';
            if (data.messages && data.messages.length > 0) {
                data.messages.forEach(function (msg, idx) {
                    let isMine = msg.senderId == window.currentUserId;
                    let msgDate = new Date(msg.sentAt);

                    // Tính ngày/tháng/năm của tin nhắn hiện tại
                    let dateStr = msgDate.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });

                    // Nếu là tin nhắn đầu tiên hoặc khác ngày với tin nhắn trước đó, chèn dòng ngày tháng
                    if (idx === 0 || dateStr !== lastDateStr) {
                        messagesHtml += `<div class="text-center text-muted my-2" style="font-size:13px;">${dateStr}</div>`;
                        lastDateStr = dateStr;
                    }

                    // Logic hiển thị giờ hoặc ngày/tháng như trước
                    let timeLabel = '';
                    timeLabel = msgDate.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });

                    let messageContent = '';
                    if (msg.content) {
                        messageContent += `<div>${msg.content}</div>`;
                    }
                    messagesHtml += `<div class="d-flex ${isMine ? 'justify-content-end' : 'justify-content-start'} mb-2">
                        <div class="p-2 rounded" style="background: ${isMine ? '#4667b3' : '#c1c4c7'}; color: #fff; max-width: 70%;">
                            ${messageContent}
                            <div class="text-muted small text-right" style="color:#fff!important">${timeLabel}</div>
                        </div>
                    </div>`;
                });
            } else {
                messagesHtml = '<div class="text-center text-muted">Chưa có tin nhắn nào.</div>';
            }
            $('#chatBox').html(messagesHtml);
            $('#chatBox').scrollTop($('#chatBox')[0].scrollHeight);
        },
        error: function(xhr) {
            if (xhr.status === 401) {
                Swal.fire('Bạn cần đăng nhập để xem hội thoại!');
            }
        }
    });
}

function loadQuickReplies() {
    $.ajax({
        url: 'https://localhost:7225/api/chatroom/quick-replies',
        method: 'GET',
        headers: {
            'Authorization': 'Bearer ' + getAccessToken()
        },
        success: function (data) {
            let html = '';
            if (data && data.length > 0) {
                data.forEach(function (reply) {
                    let content = reply.content || reply.message;
                    html += `<button class="btn btn-outline-secondary btn-sm mr-2 quick-reply-btn" type="button">${content}</button>`;
                });
            } else {
                html = '<span class="text-muted">Chưa có tin nhắn nhanh nào.</span>';
            }
            $('#quickReplyContainer').html(html);
        },
        error: function(xhr) {
            if (xhr.status === 401) {
                Swal.fire('Bạn cần đăng nhập để sử dụng tin nhắn nhanh!');
            }
        }
    });
}
// Sự kiện click quick reply (chèn vào ô nhập tin nhắn)
$(document).on('click', '.quick-reply-btn', function () {
    const text = $(this).text();
    $('#messageInput').val(text).focus();
});

function sendMessageHandler() {
    const messageInput = $('#messageInput');
    const message = messageInput.val().trim();
    if (!message || !currentConversationId || !currentReceiverId) return;
    // Chỉ gửi khi SignalR đã kết nối
    if (connection && connection.state === "Connected" && connection.invoke) {
        connection.invoke('SendMessage', currentConversationId, currentReceiverId, message, null)
            .then(() => {
                appendMessageToChat(window.currentUserId, message, null);
                messageInput.val('');
            })
            .catch(err => console.error('SendMessage error:', err));
    } else {
        console.warn('SignalR chưa kết nối, vui lòng chờ...');
    }
}

function appendMessageToChat(senderId, message, imageUrl) {
    const isMine = senderId === window.currentUserId;
    const timeOnly = new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
    let messageContent = '';
    if (message) {
        messageContent += `<div>${message}</div>`;
    }
    const html = `<div class="d-flex ${isMine ? 'justify-content-end' : 'justify-content-start'} mb-2">
        <div class="p-2 rounded" style="background: ${isMine ? '#4667b3' : '#c1c4c7'}; color: #fff; max-width: 70%;">
            ${messageContent}
            <div class="text-muted small text-right" style="color:#fff!important">${timeOnly}</div>
        </div>
    </div>`;
    $('#chatBox').append(html);
    $('#chatBox').scrollTop($('#chatBox')[0].scrollHeight);
}

function updateConversationPreview(conversationId, message) {
    // Tìm item hội thoại theo data-id
    const $item = $(`.conversation-item[data-id='${conversationId}']`);
    if ($item.length > 0) {
        // Xóa preview cũ nếu có
        $item.find('.last-message').remove();
        // Thêm preview mới
        $item.find('.ml-2').append(`<div class="last-message text-truncate small text-muted">${message}</div>`);
        // Highlight hội thoại có tin nhắn mới
        $item.addClass('bg-light');
        // Đẩy hội thoại này lên đầu danh sách (nếu muốn)
        $item.prependTo('#conversation-list');
    }
}

function initSignalR() {
    if (!window.signalR) {
        $.getScript('https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/7.0.5/signalr.min.js', startSignalR);
    } else {
        startSignalR();
    }
}

function startSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl('https://localhost:7225/chathub', {
            accessTokenFactory: () => getAccessToken()
        })
        .withAutomaticReconnect()
        .build();

    connection.on('ReceiveMessage', function (senderId, message, imageUrl, conversationId) {
        if (conversationId === currentConversationId) {
            appendMessageToChat(senderId, message, imageUrl);
        } else {
            updateConversationPreview(conversationId, message);
        }
    });

    connection.start()
        .then(() => {
            $('#sendBtn').prop('disabled', false); // Bật nút gửi khi đã kết nối
        })
        .catch(err => {
            $('#sendBtn').prop('disabled', true); // Tắt nút gửi nếu lỗi
        });

    connection.onreconnecting(() => $('#sendBtn').prop('disabled', true));
    connection.onreconnected(() => $('#sendBtn').prop('disabled', false));
    connection.onclose(() => $('#sendBtn').prop('disabled', true));
} 