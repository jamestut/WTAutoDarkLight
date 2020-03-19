# WTAutoDarkLight
This app is a stopgap solution for automatically swithing Windows Terminal's color scheme based on Windows dark or light settings, since the Windows Terminal in the current iteration is unable to automatically switch between dark or light mode.

This app has to be kept run. A scheduled task can be configured for this app to run on logon.

This app depends on `Newtonsoft.Json`.

**Warning**: this app will remove the comments in the target Windows Terminal's JSON file!

## Usage

This app takes two arguments:

1. The first argument is the path to the target Windows Terminal's JSON config file. This file will be modified upon app startup and when the app dark or light mode is changed in Windows appearance settings.
2. The second argument is the JSON file used as the reference theme settings. Please refer to the [Scheme File](#scheme-file) section for the format.

This app will continue to run until stopped, while also watching any changes to the user's app color preference. During this app's startup, or when the app color preference is changed, some values in the configuration file specified in the first argument in the `defaults` object inside the `profiles` object will be replaced with the data specified in the second argument (the [Scheme File](#scheme-file)).

## Scheme File

The scheme file must be a valid JSON file consisting of an object with two key: `dark` and `light`. Those corresponds to dark mode and light mode, respectively.

Each of those keys must be a single level object, with the keys as specified in the below table and all values must be in string.

| Key                   | Valid Value                             | Description                                  |
| --------------------- | --------------------------------------- | -------------------------------------------- |
| `colorScheme`         | A valid color scheme name.              | The color scheme that will be used.          |
| `cursorColor`         | HTML hex triplet color (e.g. `#f0f8ff`) | The color of the cursor.                     |
| `selectionBackground` | HTML hex triplet color (e.g. `#f0f8ff`) | The background color of selection highlight. |

All keys specified in the above table are optional. If they're not specified, then the Windows Terminal's or color scheme's defaults will be used.

Here is an example of the configuration JSON file:

```json
{
    "dark": {
        "colorScheme":"One Half Dark"
    },
    "light": {
        "colorScheme": "One Half Light",
        "cursorColor": "#333333",
        "selectionBackground": "#333333"
    }
}
```

