using System.Runtime.InteropServices;

public static class ReinterpretExtensions
{
	//类似于联合体，内存数据相同，可以用不同的数据类型
	[StructLayout(LayoutKind.Explicit)]
	struct IntFloat
	{

		[FieldOffset(0)]
		public int intValue;

		[FieldOffset(0)]
		public float floatValue;
	}

	public static float ReinterpretAsFloat(this int value)
	{
		IntFloat converter = default;
		converter.intValue = value;
		return converter.floatValue;
	}
}