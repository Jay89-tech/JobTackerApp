"use strict";

// Create SignalR connection
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/jobtrackerhub")
    .withAutomaticReconnect()
    .build();

// Start connection
connection.start().then(function () {
    console.log("Connected to JobTracker Hub");

    // Show connection status
    showNotification("Connected to real-time updates", "success");
}).catch(function (err) {
    console.error("Failed to connect to JobTracker Hub: " + err.toString());
    showNotification("Failed to connect to real-time updates", "warning");
});

// Handle connection events
connection.onreconnecting(() => {
    console.log("Reconnecting to JobTracker Hub...");
    showNotification("Reconnecting to real-time updates...", "info");
});

connection.onreconnected(() => {
    console.log("Reconnected to JobTracker Hub");
    showNotification("Reconnected to real-time updates", "success");
});

connection.onclose(() => {
    console.log("Disconnected from JobTracker Hub");
    showNotification("Disconnected from real-time updates", "warning");
});

// Handle real-time notifications
connection.on("JobCreated", function (notification) {
    console.log("Job Created:", notification);
    showJobNotification(notification, "New Job Posted!");

    // Refresh jobs list if on jobs page
    if (window.location.pathname.includes('/Jobs')) {
        setTimeout(() => location.reload(), 2000);
    }
});

connection.on("JobUpdated", function (notification) {
    console.log("Job Updated:", notification);
    showJobNotification(notification, "Job Updated!");

    // Update job card if visible
    updateJobCard(notification);
});

connection.on("JobDeleted", function (notification) {
    console.log("Job Deleted:", notification);
    showNotification(`Job with ID ${notification.JobId} has been deleted`, "info");

    // Remove job card if visible
    removeJobCard(notification.JobId);
});

connection.on("ApplicationCreated", function (notification) {
    console.log("Application Created:", notification);
    showNotification(`New application: ${notification.ApplicantName} applied for ${notification.JobTitle}`, "info");

    // Update application counter if on job details
    updateApplicationCounter(notification.JobId);
});

connection.on("ApplicationStatusChanged", function (notification) {
    console.log("Application Status Changed:", notification);
    showNotification(`Application status changed to ${notification.Status}`, "info");

    // Update application status in UI
    updateApplicationStatus(notification);
});

connection.on("UserNotification", function (notification) {
    console.log("User Notification:", notification);
    showNotification(notification.Message, notification.NotificationType);
});

connection.on("GlobalNotification", function (notification) {
    console.log("Global Notification:", notification);
    showNotification(notification.Message, notification.NotificationType);
});

// Utility functions
function showNotification(message, type = "info") {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
    notification.style.cssText = 'top: 20px; right: 20px; z-index: 1050; max-width: 300px;';

    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

    document.body.appendChild(notification);

    // Auto-remove after 5 seconds
    setTimeout(() => {
        if (notification.parentNode) {
            notification.remove();
        }
    }, 5000);
}

function showJobNotification(notification, title) {
    const message = `
        <strong>${title}</strong><br>
        <strong>${notification.Title}</strong><br>
        ${notification.Company} - ${notification.Location}<br>
        ${notification.Salary ? ' + notification.Salary.toLocaleString() : 'Salary not specified'}
        `;
    
    showNotification(message, "primary");
}

function updateJobCard(notification) {
    const jobCard = document.querySelector(`[data - job - id= "${notification.JobId}"]`);
    if (jobCard) {
        // Update status badge
        const statusBadge = jobCard.querySelector('.job-status');
        if (statusBadge) {
            statusBadge.textContent = notification.Status;
            statusBadge.className = `badge bg - ${ getStatusColor(notification.Status) } job - status`;
        }
        
        // Update timestamp
        const timestamp = jobCard.querySelector('.job-updated');
        if (timestamp) {
            timestamp.textContent = 'Updated: ' + new Date(notification.UpdatedAt).toLocaleDateString();
        }
    }
}

function removeJobCard(jobId) {
    const jobCard = document.querySelector(`[data - job - id="${jobId}"]`);
    if (jobCard) {
        jobCard.style.transition = 'opacity 0.3s';
        jobCard.style.opacity = '0';
        setTimeout(() => jobCard.remove(), 300);
    }
}

function updateApplicationCounter(jobId) {
    const counter = document.querySelector(`[data - job - id= "${jobId}"] .application - count`);
    if (counter) {
        const currentCount = parseInt(counter.textContent) || 0;
        counter.textContent = currentCount + 1;
    }
}

function updateApplicationStatus(notification) {
    const appRow = document.querySelector(`[data - application - id="${notification.ApplicationId}"]`);
    if (appRow) {
        const statusBadge = appRow.querySelector('.application-status');
        if (statusBadge) {
            statusBadge.textContent = notification.Status;
            statusBadge.className = `badge bg - ${ notification.StatusColor || 'primary' } application - status`;
        }
    }
}

function getStatusColor(status) {
    switch (status?.toLowerCase()) {
        case 'open': return 'success';
        case 'closed': return 'secondary';
        case 'filled': return 'primary';
        case 'onhold': return 'warning';
        default: return 'light';
    }
}

// Join specific job group when viewing job details
function joinJobGroup(jobId) {
    if (connection.state === signalR.HubConnectionState.Connected) {
        connection.invoke("JoinJobGroup", jobId.toString());
    }
}

// Leave job group
function leaveJobGroup(jobId) {
    if (connection.state === signalR.HubConnectionState.Connected) {
        connection.invoke("LeaveJobGroup", jobId.toString());
    }
}

// Export functions for global use
window.jobTrackerHub = {
    joinJobGroup,
    leaveJobGroup,
    showNotification
};