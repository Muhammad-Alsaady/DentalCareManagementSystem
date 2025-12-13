console.log('Treatment Plan JS Loaded');

let items = [];

window.addService = function () {

    console.log('Add Service Clicked');

    const ddl = document.getElementById('serviceDropdown');
    const qtyInput = document.getElementById('serviceQuantity');

    if (!ddl || !ddl.value) {
        alert('Select service first');
        return;
    }

    const opt = ddl.options[ddl.selectedIndex];
    const id = ddl.value;
    const name = opt.dataset.name;
    const price = parseFloat(opt.dataset.price);
    const qty = parseInt(qtyInput.value) || 1;

    const existing = items.find(x => x.id === id);
    if (existing) {
        existing.qty += qty;
    } else {
        items.push({ id, name, price, qty });
    }

    ddl.value = '';
    qtyInput.value = 1;

    render();
};

window.render = function () {

    const table = document.getElementById('servicesTable');
    const body = document.getElementById('servicesBody');
    const noServices = document.getElementById('noServices');

    body.innerHTML = '';

    if (items.length === 0) {
        table.style.display = 'none';
        noServices.style.display = 'block';
        calculate();
        return;
    }

    table.style.display = '';
    noServices.style.display = 'none';

    items.forEach((x, i) => {
        body.innerHTML += `
            <tr>
                <td>${x.name}</td>
                <td>$${x.price.toFixed(2)}</td>
                <td>${x.qty}</td>
                <td>$${(x.price * x.qty).toFixed(2)}</td>
                <td>
                    <button class="btn btn-danger btn-sm"
                        onclick="removeItem(${i})">X</button>
                </td>
            </tr>
        `;
    });

    calculate();
};

window.removeItem = function (i) {
    items.splice(i, 1);
    render();
};

window.calculate = function () {

    const total = items.reduce((s, x) => s + x.price * x.qty, 0);
    const discount = parseFloat(document.getElementById('discount').value) || 0;
    const paid = parseFloat(document.getElementById('paid').value) || 0;

    const discountValue = total * discount / 100;
    const net = total - discountValue;
    const remaining = Math.max(0, net - paid);

    document.getElementById('total').innerText = `$${total.toFixed(2)}`;
    document.getElementById('discountValue').innerText = `$${discountValue.toFixed(2)}`;
    document.getElementById('net').innerText = `$${net.toFixed(2)}`;
    document.getElementById('remaining').innerText = `$${remaining.toFixed(2)}`;
};
window.savePayment = function () {
    const form = document.getElementById('paymentForm');
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }
    form.submit();
};
window.saveTreatmentPlan = function () {

    if (items.length === 0) {
        alert('Please add at least one service');
        return;
    }

    const form = document.getElementById('treatmentPlanForm');

    // امسح أي hidden inputs قديمة
    form.querySelectorAll('.treatment-hidden').forEach(e => e.remove());

    items.forEach((item, index) => {

        const nameInput = document.createElement('input');
        nameInput.type = 'hidden';
        nameInput.name = `TreatmentItems[${index}].Name`;
        nameInput.value = item.name;
        nameInput.classList.add('treatment-hidden');

        const priceInput = document.createElement('input');
        priceInput.type = 'hidden';
        priceInput.name = `TreatmentItems[${index}].Price`;
        priceInput.value = item.price;
        priceInput.classList.add('treatment-hidden');

        const qtyInput = document.createElement('input');
        qtyInput.type = 'hidden';
        qtyInput.name = `TreatmentItems[${index}].Quantity`;
        qtyInput.value = item.qty;
        qtyInput.classList.add('treatment-hidden');

        form.appendChild(nameInput);
        form.appendChild(priceInput);
        form.appendChild(qtyInput);
    });

    // الخصم والمدفوع
    form.appendChild(hidden('DiscountPercentage', document.getElementById('discount').value));
    form.appendChild(hidden('PaidAmount', document.getElementById('paid').value));

    form.submit();
};

function hidden(name, value) {
    const i = document.createElement('input');
    i.type = 'hidden';
    i.name = name;
    i.value = value;
    i.classList.add('treatment-hidden');
    return i;
}


