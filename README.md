# Html Css Classes Completion
Improves the existing IntelliSense of css classes in Visual Studio 2019.

Features:
- Works in any project type.
- Scans css files in the entire project structure, including referenced projects.
- External css files, which are linked via the <link> attribute in any html/cshtml file, will be scanned as well.
- Isolated css support. Css classes from \*.razor.css files, will only be shown in the corresponding \*.razor component.

## How To Use:
After installing, the extension should scan for .css files automatically once all projects and the extension are fully loaded.
You can see the progress in the bottom left corner.

![image](https://user-images.githubusercontent.com/43059964/128539157-986cf9d9-e76f-452f-b2c9-c0867e61a478.png)

If you add new css files, you can re-scan all files by using Tools -> Scan all Projects for CSS Classes

![image](https://user-images.githubusercontent.com/43059964/128539310-d21a2859-8ed9-4208-a956-55c14c3a9fec.png)

You should now be able to use the intellisense:

![image](https://user-images.githubusercontent.com/43059964/128539514-825f6282-2a02-468f-8ec6-abd622fc5ad5.png)
