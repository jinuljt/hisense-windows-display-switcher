using HisenseDisplaySwitcher;

var hdmi4 = TvState.Parse("PORT=1284\nSOURCE=4\nmWakefulness=Awake");
if (!hdmi4.IsTarget(1284)) throw new Exception("Expected active target HDMI.");

var hdmi3 = TvState.Parse("PORT=1283\nSOURCE=4\nmWakefulness=Awake");
if (hdmi3.IsTarget(1284)) throw new Exception("Wrong HDMI must not be selected.");

var asleep = TvState.Parse("PORT=1284\nSOURCE=4\nmWakefulness=Asleep");
if (asleep.IsTarget(1284)) throw new Exception("Sleeping TV must not be selected.");

Console.WriteLine("Logic checks passed.");

