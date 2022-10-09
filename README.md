# Html Css Class Completion
The existing HTML/Razor intellisense for Visual Studio has a lot of limitations. It only works if your project is a web-based project, and does not support razor class libraries. It only scans for .css files in the wwwroot directory, and it doesn't support anything fancy like the relatively new isolated CSS feature for razor components.

This extension fixes all that, by improving the existing HTML intellisense with the following features:

- Works in any project type.
- Scans CSS files in the entire project structure, including referenced projects, as well as nuget packages.
- External CSS files, which are linked via the \<link> attribute in any .html/.cshtml file, will be scanned as well.
- Isolated CSS support. CSS classes from \*.razor.css files, will only be shown in the corresponding \*.razor component.

Visual Studio Marketplace: https://marketplace.visualstudio.com/items?itemName=KevinMueller.HtmlCssClassCompletion22

## How To Use:
After installing, the extension should scan for .css files automatically once all projects and the extension are fully loaded.
You can see the progress in the bottom left corner.

![image](https://user-images.githubusercontent.com/43059964/163576302-67e3ecd9-478c-47f7-92c9-48b1d1b894b8.png)

If you add new css files, you can re-scan all files by using Tools -> Scan all Projects for CSS Classes.

![image](https://user-images.githubusercontent.com/43059964/128539310-d21a2859-8ed9-4208-a956-55c14c3a9fec.png)

The scanning also happens automatically every time you save a .css or .html file. This behavior can be turned off in the options under Tools -> Options -> Better Razor Css Class Intellisense.

You should now be able to use the intellisense:

![image](https://user-images.githubusercontent.com/43059964/163576675-7b019d9c-f14c-4a8e-b594-8f241da01298.png)

## Have a feature idea / found a bug?
Feel free to create an issue in this repo. :)
