﻿async function collectMainFormData() {
    const form = document.getElementById('filterForm');
    const data = {};

    const elements = form.querySelectorAll('input, select, textarea');

    elements.forEach(el => {
        const { name, type, value, checked, tagName } = el;
        if (!name || name === "__RequestVerificationToken") return;

        const key = name.endsWith('[]') ? name.slice(0, -2) : name;

        if (type === 'checkbox') {
            if (name.endsWith('[]')) {
                if (!data[key]) data[key] = [];
                if (checked) data[key].push(value);
            } else {
                data[key] = checked;
            }
        } else if (type === 'radio') {
            if (checked) {
                data[key] = value;
            }
        } else if (tagName === 'SELECT' && el.multiple) {
            const selectedValues = Array.from(el.selectedOptions).map(opt => opt.value);
            data[key] = selectedValues;
        } else {
            data[key] = value;
        }
    });

    const addressDisplay = document.getElementById('selectedAddressDisplay');
    const addressValue = addressDisplay ? addressDisplay.innerText.trim() : '';
    if (addressValue && addressValue !== 'Chọn địa chỉ') {
        data['address'] = addressValue;
    }

    if (uploadedImages.length > 0) {  //trong load-muitl-imgs
        data['images'] = uploadedImages.map(img => img.file);
    }

    const activePackageCard = document.querySelector('.package-type-card.border-primary.border-3');
    if (activePackageCard) {
        data['packageTypeId'] = parseInt(activePackageCard.dataset.id);
        data['packageTypeName'] = activePackageCard.dataset.name;
    }

    const durationRadio = document.querySelector('input[name="duration"]:checked');
    if (durationRadio) {
        data['duration'] = parseInt(durationRadio.dataset.duration);
    }

    const timeUnitSelect = document.getElementById('timeUnitDropdown');
    if (timeUnitSelect) {
        data['timeUnitId'] = parseInt(timeUnitSelect.value);
    }

    const totalPriceText = document.getElementById("summaryTotalAmount").innerText;
    const priceCleaned = totalPriceText.replace(/[^\d]/g, '');
    if (priceCleaned) {
        data['totalPrice'] = parseInt(priceCleaned);
    }
    let rawPrice = document.getElementById("priceInput").value;
    let cleanPrice = parseInt(rawPrice.replace(/\./g, ""));
    data['Price'] = parseInt(cleanPrice);

    const startDateInput = document.getElementById('startDate');
    if (startDateInput) {
        data['startDate'] = startDateInput.value;
        if (data.startDate && data.duration && data.timeUnitId) {
            const start = new Date(data.startDate + "T00:00:00");
            if (!isNaN(start)) {
                let end = new Date(start);
                switch (data.timeUnitId) {
                    case 1: end.setDate(start.getDate() + data.duration); break;
                    case 2: end.setDate(start.getDate() + data.duration * 7); break;
                    case 3: end.setMonth(start.getMonth() + data.duration); break;
                }
                data['endDate'] = end.toISOString().split('T')[0];
            }
        }
    }

    const scheduleSelect = document.getElementById('scheduleSelect');
    if (scheduleSelect && !scheduleSelect.disabled) {
        const selectedHour = scheduleSelect.value;
        if (selectedHour !== 'Đăng ngay bây giờ') {
            data['scheduleTime'] = selectedHour;
        }
    }

    const selectedPromotion = document.querySelector('.promotion-item.selected');
    if (selectedPromotion) {
        data['promotionId'] = selectedPromotion.dataset.id || selectedPromotion.innerText.trim();
    }

    const activeAmenities = document.querySelectorAll('.amenity-btn.active[data-id]');
    data['amenities'] = Array.from(activeAmenities).map(btn => btn.dataset.id);

    if (data.timeUnitId && data.packageTypeId && data.duration) {
        try {
            const response = await fetch('/api/v1/get-pricing', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({
                    timeUnitId: data.timeUnitId,
                    packageTypeId: data.packageTypeId,
                    durationValue: data.duration
                })
            });

            if (response.ok) {
                const result = await response.json();
                data['pricingId'] = result.pricingId;
            }
            else {
                console.warn('Không tìm thấy pricingId phù hợp');
            }
        } catch (error) {
            console.error('Lỗi khi lấy pricingId:', error);
        }
    }

    return data;
}

//submit thanh toán
document.getElementById('testCollectFormBtn')?.addEventListener('click', async function () {
    const loadingOverlay = document.getElementById('paymentLoadingOverlay');
    loadingOverlay?.classList.remove('d-none');
    loadingOverlay?.classList.add('d-flex');

    await new Promise(resolve => setTimeout(resolve, 1500));

    const form = document.getElementById('filterForm');
    const rawData = await collectMainFormData();
    const formData = new FormData();

    if (rawData.Images) { 
        rawData.Images.forEach(file => {
            formData.append("Images", file);
        });
        delete rawData.Images;
    }


    for (const key in rawData) {
        const value = rawData[key];
        if (Array.isArray(value)) {
            value.forEach(v => formData.append(key, v));
        } else {
            formData.set(key, value.toString());
        }
    }

    const selectedAmenitiesRaw = form.querySelector('[name="selectedAmenities"]')?.value;
    if (selectedAmenitiesRaw) {
        const ids = selectedAmenitiesRaw.split(',').map(x => x.trim()).filter(x => x !== '');
        ids.forEach(id => {
            formData.append("AmenityIds", id);
        });
        formData.delete("selectedAmenities");
    }

    formData.set("totalPrice", parseFloat(rawData.totalPrice));
    formData.set("myPrice", parseFloat(rawData.Price).toString());
    formData.set("startDate", rawData.startDate);
    formData.set("endDate", rawData.endDate);
    formData.set("accommodationTypeId", parseInt(rawData.category));
    formData.set("address", rawData.address);
    formData.set("pricingId", rawData.pricingId);

    console.log(parseFloat(rawData.totalPrice));

    try {
        const response = await fetch('/nguoi-cho-thue/dang-tin', {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
            throw new Error("Lỗi kết nối đến server");
        }

        const result = await response.json();

        if (result.success) {
            document.getElementById('hiddenPostId').value = result.postId;
            document.getElementById('hiddenAmountInputPayOS').value = result.amount;

            document.getElementById('realPayOsSubmit').click();
        } else {
            showToast('Đăng bài không thành công. Vui lòng kiểm tra lại dữ liệu.');
        }

    } catch (err) {
        console.error(err);
        showToast('Có lỗi xảy ra khi gửi dữ liệu. Vui lòng thử lại.');
    } finally {
        loadingOverlay?.classList.remove('d-flex');
        loadingOverlay?.classList.add('d-none');
    }
});