function copyToClipboard(text, event) {
        navigator.clipboard.writeText(text).then(function (event) {
            //alert("Copied to clipboard!");
            
        })
            .catch(function (error) {
                alert(error);
            });
    //event.preventDefault();
    return false;
    }
