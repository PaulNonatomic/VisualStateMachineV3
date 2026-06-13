namespace Nonatomic.VSM.Attributes
{
	/// <summary>
	/// Constants for node icon names.
	/// Use these with StateStyleAttribute to set icons on state nodes.
	/// Icons are loaded from Editor/Resources/Icons/Nodes/
	/// </summary>
	/// <example>
	/// <code>
	/// [StateStyle(Color = StateColors.Blue, Icon = NodeIcon.Clock)]
	/// public class DelayState : State { }
	/// </code>
	/// </example>
	public static class NodeIcon
	{
		// Basic shapes
		public const string Circle = "circle";
		public const string Hexagon = "hexagon";
		public const string Pentagon = "pentagon";
		public const string Triangle = "triangle";
		public const string Center = "center";
		public const string CenterAlign = "center-align";
		public const string CenterSquare = "center-square";

		// Flow control
		public const string Enter = "enter";
		public const string Exit = "exit";
		public const string Eject = "eject";
		public const string Play = "play";
		public const string Share = "share";
		public const string Random = "random";
		public const string Command = "command";

		// Time
		public const string Clock = "clock";
		public const string Hourglass = "hourglass";

		// Communication
		public const string BroadcastLeft = "broadcast-left";
		public const string BroadcastRight = "broadcast-right";
		public const string BroadcastHaloLeft = "broadcast-halo-left";
		public const string BroadcastHaloRight = "broadcast-halo-right";
		public const string Post = "post";
		public const string PostSolid = "post-solid";

		// Objects
		public const string Cube = "cube";
		public const string Box3dCenter = "box3d-center";
		public const string Grid = "grid";
		public const string GridPerspective = "grid-perspective";

		// UI/Info
		public const string Note = "note";
		public const string Report = "report";
		public const string Menu = "menu";
		public const string Help = "help";
		public const string HelpCircle = "help-circle";
		public const string InfoCircle = "info-circle";
		public const string Warning = "warning";
		public const string ClipboardCheck = "clipboard-check";

		// Actions
		public const string Pencil = "pencil";
		public const string Erase = "erase";
		public const string Trash = "trash";
		public const string Download = "download";
		public const string DownloadCircle = "download-circle";
		public const string FloppyDisk = "floppy-disk";
		public const string Folder = "folder";
		public const string Lock = "padlock";
		public const string Unlock = "padlock-open";
		public const string Home = "home";
		public const string User = "user";
		public const string Path = "path-arrow";
		public const string Run = "running";
		public const string Shield = "historic-shield";
		public const string ArrowLeft = "arrow-left";
		public const string ArrowRight = "arrow-right";
		public const string ArrowUp = "arrow-up";
		public const string ArrowDown = "arrow-down";
		public const string Restart = "restart";
		public const string Xmark = "xmark-circle";
		public const string Boxing = "boxing-glove";
		public const string LightBulb = "light-bulb";
		public const string LightBulbOn = "light-bulb-on";
		public const string LightBulbOff = "light-bulb-off";

		// Checks
		public const string CheckCircle = "check-circle";
		public const string CheckCircleSolid = "check-circle-solid";
		public const string PlusCircle = "plus-circle";
		public const string PlusSquare = "plus-square";

		// 3D/Geometry
		public const string Arc3d = "arc3d";
		public const string Arc3dCenterPoint = "arc3d-center-point";
		public const string Bridge3d = "bridge3d";
		public const string CurveArray = "curve-array";
		public const string Extrude = "extrude";
		public const string OnePointCircle = "one-point-circle";
		public const string RulerArrows = "ruler-arrows";
		public const string Spiral = "spiral";
		public const string XrayView = "xray-view";

		// Sports/Games
		public const string Archery = "archery";
		public const string ArrowArchery = "arrow-archery";
		public const string Basketball = "basketball";
		public const string BoxingGlove = "boxing-glove";
		public const string Fishing = "fishing";
		public const string Golf = "golf";
		public const string Trophy = "trophy";
		public const string Medal = "medal";
		public const string LeaderboardStar = "leaderboard-star";

		// Media
		public const string Film = "film";
		public const string Eye = "eye";
		public const string Sparks = "sparks";
		public const string Flower = "flower";
	}
}
