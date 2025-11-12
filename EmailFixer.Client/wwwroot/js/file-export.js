/**
 * Safe file export functions - replaces unsafe eval()
 */

// Export CSV file without using eval()
window.exportToCSV = function(content, filename) {
    try {
        // Create blob from content
        const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });

        // Create object URL (much safer than eval)
        const url = URL.createObjectURL(blob);

        // Create and trigger download
        const link = document.createElement('a');
        link.setAttribute('href', url);
        link.setAttribute('download', filename);
        link.style.visibility = 'hidden';

        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        // Clean up
        URL.revokeObjectURL(url);

        console.log(`File exported successfully: ${filename}`);
        return true;
    } catch (error) {
        console.error('Export failed:', error);
        return false;
    }
};

// Export JSON file
window.exportToJSON = function(data, filename) {
    try {
        const content = typeof data === 'string' ? data : JSON.stringify(data, null, 2);
        const blob = new Blob([content], { type: 'application/json' });

        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.setAttribute('href', url);
        link.setAttribute('download', filename);
        link.style.visibility = 'hidden';

        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        URL.revokeObjectURL(url);
        return true;
    } catch (error) {
        console.error('Export failed:', error);
        return false;
    }
};

// Export as text file
window.exportToText = function(content, filename) {
    try {
        const blob = new Blob([content], { type: 'text/plain' });

        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.setAttribute('href', url);
        link.setAttribute('download', filename);
        link.style.visibility = 'hidden';

        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        URL.revokeObjectURL(url);
        return true;
    } catch (error) {
        console.error('Export failed:', error);
        return false;
    }
};
