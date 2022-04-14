# Html Css Class Completion
The existing HTML intellisense for Visual Studio has a lot of limitations. It only works if your project is a web-based project, and does not support razor class libraries. It only scans for .css files in the wwwroot directory, and it doesn't support anything fancy like the relatively new isolated CSS feature for razor components.

This extension fixes all that, by improving the existing HTML intellisense with the following features:

- Works in any project type.
- Scans CSS files in the entire project structure, including referenced projects, as well as nuget packages.
- External CSS files, which are linked via the \<link> attribute in any .html/.cshtml file, will be scanned as well.
- Isolated CSS support. CSS classes from \*.razor.css files, will only be shown in the corresponding \*.razor component.

The extension is currently in a very basic state, but the functionality is there.

Visual Studio Marketplace: https://marketplace.visualstudio.com/items?itemName=KevinMueller.BetterHtmlRazorCssClassIntellisense

## How To Use:
After installing, the extension should scan for .css files automatically once all projects and the extension are fully loaded.
You can see the progress in the bottom left corner.

![image](https://user-images.githubusercontent.com/43059964/128539157-986cf9d9-e76f-452f-b2c9-c0867e61a478.png)

If you add new css files, you can re-scan all files by using Tools -> Scan all Projects for CSS Classes

![image](https://user-images.githubusercontent.com/43059964/128539310-d21a2859-8ed9-4208-a956-55c14c3a9fec.png)

You should now be able to use the intellisense:

![image](https://user-images.githubusercontent.com/43059964/128539514-825f6282-2a02-468f-8ec6-abd622fc5ad5.png)

## Have a feature idea / found a bug?
Feel free to create an issue in this repo. :)
