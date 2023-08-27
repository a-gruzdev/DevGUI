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

### Available DevGUI Widgets

```csharp
float Slider(string title, float value, float min, float max);
string TextField(string title, string text);
int IntField(string title, int value);
float FloatField(string title, float value);
Vector2 Vector2Field(string title, Vector2 value);
Vector3 Vector3Field(string title, Vector3 value);
Vector4 Vector4Field(string title, Vector4 value);
Vector2Int Vector2IntField(string title, Vector2Int value);
Vector3Int Vector3IntField(string title, Vector3Int value);
Color ColorField(string title, Color value);
T EnumField<T>(string title, T enumValue); //supports flags
```
