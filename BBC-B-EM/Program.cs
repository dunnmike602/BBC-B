using MLDComputing.Emulators.BBCSim.Beeb;

var em = new BeebEm();
var osPath = Path.Combine(AppContext.BaseDirectory, "roms", string.Intern("os12.rom"));
var basicPath = Path.Combine(AppContext.BaseDirectory, "roms", "basic2.rom");

em.LoadRoms(osPath, basicPath);
em.Start();