# Dev Tools

Ingame developer tools panel

## Usage

`namespace DevTools`

### Adding GUI

```csharp
private void OnEnable() => DevGUI.AddGUI("Example", OnDevGUI);
private void OnDisable() => DevGUI.RemoveGUI("Example", OnDevGUI);

private void OnDevGUI()
{
    // Unity imgui code
}
```

### Button

```csharp
if (GUILayout.Button("Button"))
{
    //button logic
}
```

### Label

```csharp
GUILayout.Label("Label");
GUILayout.Label($"FPS: {1f / Time.unscaledDeltaTime}");
```

### Toggle

```csharp
someBool = GUILayout.Toggle(someBool, "Toggle Example");
```

```csharp
var value = GUILayout.Toggle(target.activeSelf, "Toggle Example");
if (value != target.activeSelf)
{
    target.SetActive(value);
}
```

### Slider

```csharp
floatValue = DevGUI.Slider("Float Slider", floatValue, 0f, 1f);
```

### TextField

```csharp
strValue = DevGUI.TextField("Text Field", strValue);
```

### EnumField

supports enum flags

```csharp
state = DevGUI.EnumField("State", state);
```

## Work In Progress

- Float field
- Int field
- Color field / Picker
