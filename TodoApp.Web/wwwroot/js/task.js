// Task-specific JavaScript for AJAX operations

document.addEventListener('DOMContentLoaded', function () {
    // Handle toggle form submissions via AJAX
    document.querySelectorAll('.toggle-form').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            handleToggle(form);
        });
    });

    // Handle delete form submissions via AJAX
    document.querySelectorAll('.delete-form').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();

            if (confirm('Are you sure you want to delete this task?')) {
                handleDelete(form);
            }
        });
    });
});

function handleToggle(form) {
    var taskItem = form.closest('.task-item');
    var toggleBtn = form.querySelector('.toggle-btn');
    var icon = toggleBtn.querySelector('i');

    // Add loading state
    taskItem.classList.add('loading');

    fetch(form.action, {
        method: 'POST',
        headers: {
            'X-Requested-With': 'XMLHttpRequest',
            'RequestVerificationToken': getAntiForgeryToken()
        }
    })
    .then(function (response) {
        return response.json();
    })
    .then(function (data) {
        if (data.success) {
            // Update UI
            if (data.completed) {
                taskItem.classList.add('task-completed');
                icon.className = 'bi bi-check-circle-fill text-success fs-4';

                // Add strikethrough to title and description
                var title = taskItem.querySelector('h6');
                var description = taskItem.querySelector('p.small');
                if (title) {
                    title.classList.add('text-decoration-line-through', 'text-muted');
                }
                if (description) {
                    description.classList.add('text-decoration-line-through');
                }
            } else {
                taskItem.classList.remove('task-completed');
                icon.className = 'bi bi-circle text-secondary fs-4';

                // Remove strikethrough
                var title = taskItem.querySelector('h6');
                var description = taskItem.querySelector('p.small');
                if (title) {
                    title.classList.remove('text-decoration-line-through', 'text-muted');
                }
                if (description) {
                    description.classList.remove('text-decoration-line-through');
                }
            }
        }
    })
    .catch(function (error) {
        console.error('Error:', error);
    })
    .finally(function () {
        taskItem.classList.remove('loading');
    });
}

function handleDelete(form) {
    var taskItem = form.closest('.task-item');

    // Add loading state
    taskItem.classList.add('loading');

    fetch(form.action, {
        method: 'POST',
        headers: {
            'X-Requested-With': 'XMLHttpRequest',
            'RequestVerificationToken': getAntiForgeryToken()
        }
    })
    .then(function (response) {
        return response.json();
    })
    .then(function (data) {
        if (data.success) {
            // Animate removal
            taskItem.classList.add('fade-out');
            setTimeout(function () {
                taskItem.remove();

                // Check if task list is empty and show message
                var taskList = document.getElementById('task-list');
                if (taskList && taskList.children.length === 0) {
                    var emptyMessage = document.createElement('div');
                    emptyMessage.className = 'text-center py-5 text-muted';
                    emptyMessage.innerHTML = '<i class="bi bi-inbox fs-1"></i><p class="mt-3">No tasks yet. Add one to get started!</p>';
                    taskList.parentNode.appendChild(emptyMessage);
                    taskList.remove();
                }
            }, 300);
        }
    })
    .catch(function (error) {
        console.error('Error:', error);
        taskItem.classList.remove('loading');
    });
}

function getAntiForgeryToken() {
    var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}
