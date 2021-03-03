window.panoramicDataDemo = {

	downloadFiles: function (args) {
		for (var i = 0; i < args.items.length; i++) {
			var url = "/files/download?path=" + args.items[i].path;
			panoramicData.downloadFromUrl(url, args.items[i].name);
		}
	},

	initDropzone: function (idSelector, opt) {
		console.dir(opt);
		var myDropzone = new Dropzone(idSelector, {
			url: "/files/upload",
			params: function (files, xhr) {
				console.dir(files);
				console.dir(xhr);
				return {
					"Key": "123",
					"Path": files[0].fullPath || files[0].name
				};
			}
		});
	}
}