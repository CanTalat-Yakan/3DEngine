using System.Collections.Generic;

namespace UltralightNet.Test;

public unsafe class UlStringTest
{
	public static IEnumerable<object[]> GetTestStrings()
	{
		yield return new object[] { string.Empty };
		yield return new object[] { "я тесто я тесто aаaаaаaа!!!123!!!" };
	}

	[Theory]
	[MemberData(nameof(GetTestStrings))]
	public void SameAsNative(string str)
	{
		var reference = Methods.ulCreateStringUTF16(str, (nuint)str.Length);
		using UlString managed = new(str);

		Assert.NotEqual((nuint)0, (nuint)reference);
		Assert.Equal(reference->length, managed.length);

		Assert.NotEqual((nuint)0, (nuint)reference->data);
		Assert.NotEqual((nuint)0, (nuint)managed.data);

		for (nuint i = 0; i < reference->length; i++)
		{
			Assert.Equal(reference->data[i], managed.data[i]);
		}

		Assert.Equal(0, reference->data[reference->length]);
		Assert.Equal(0, managed.data[managed.length]);

		Methods.ulDestroyString(reference);
	}

	[Theory]
	[MemberData(nameof(GetTestStrings))]
	public void NullTerminator(string str)
	{
		var reference = Methods.ulCreateStringUTF16(str, (nuint)str.Length);
		if (reference->data is not null ? reference->data[reference->length] is not 0 : true) throw new InvalidProgramException("data[length] != 0");

		using UlString managed = new(str);

		Assert.Equal(0, managed.data[managed.length]);
		Assert.NotEqual((nuint)0, (nuint)reference->data);

		Methods.ulDestroyString(reference);
	}

	#region ASSIGN
	[Theory]
	[MemberData(nameof(GetTestStrings))]
	public void ManagedAssignString(string str){
		using UlString s = new();
		s.Assign(str);
		Assert.Equal(str, (string)s);
	}
	[Theory]
	[MemberData(nameof(GetTestStrings))]
	public void ManagedAssignManagedToNative(string str)
	{
		using UlString managed = new(str);
		var native = Methods.ulCreateStringUTF16("", 0);

		Assert.NotEqual((nuint)0, (nuint)native);
		Assert.NotEqual((nuint)0, (nuint)native->data);
		Assert.Equal((nuint)0, native->length);
		Assert.Equal(0, native->data[0]);

		native->Assign(managed);
		native->Assign(&managed);

		Assert.Equal((ulong)managed.length, (ulong)native->length);

		for (nuint i = 0; i < managed.length; i++)
		{
			Assert.Equal(managed.data[i], native->data[i]);
		}

		Methods.ulDestroyString(native);
	}
	[Theory]
	[MemberData(nameof(GetTestStrings))]
	public void ManagedAssignNativeToManaged(string str)
	{
		var native = Methods.ulCreateStringUTF16(str, 0);
		using UlString managed = new();

		managed.Assign(native);
		managed.Assign(*native);

		Assert.Equal((ulong)native->length, (ulong)managed.length);

		for (nuint i = 0; i < native->length; i++)
		{
			Assert.Equal(native->data[i], managed.data[i]);
		}

		Methods.ulDestroyString(native);
	}
	[Theory]
	[MemberData(nameof(GetTestStrings))]
	public void UNManagedAssignManagedToNative(string str)
	{
		using UlString managed = new(str);
		var native = Methods.ulCreateStringUTF16("", 0);

		Methods.ulStringAssignString(native, &managed);

		Assert.Equal((ulong)managed.length, (ulong)native->length);

		for (nuint i = 0; i < managed.length; i++)
		{
			Assert.Equal(managed.data[i], native->data[i]);
		}

		Methods.ulDestroyString(native);
	}
	[Theory]
	[MemberData(nameof(GetTestStrings))]
	public void UNManagedAssignNativeToManaged(string str)
	{
		var native = Methods.ulCreateStringUTF16(str, (nuint)str.Length);
		using UlString managed = new();

		Assert.Equal((nuint)0, managed.length);
		Assert.NotEqual((nuint)0, (nuint)managed.data);

		Methods.ulStringAssignString(&managed, native);

		Assert.Equal((nuint)native->length, (nuint)managed.length);

		for (nuint i = 0; i < native->length; i++)
		{
			Assert.Equal(native->data[i], managed.data[i]);
		}

		Methods.ulDestroyString(native);
	}
	#endregion ASSIGN

	[Theory]
	[MemberData(nameof(GetTestStrings))]
	public void AllocationTest(string inStr){
		static void Test(in string str, bool managedAllocation, bool managedDeallocation){
			UlString* u;
			if(managedAllocation) u = new UlString(str).Allocate();
			else u = Methods.ulCreateStringUTF16(str, (nuint)str.Length);

			Assert.NotEqual((nuint)0, (nuint)u);
			Assert.NotEqual((nuint)0, (nuint)u->data);

			if(managedDeallocation) u->Deallocate();
			else Methods.ulDestroyString(u);
		}

		Test(inStr, false, false);
		Test(inStr, false, true);
		Test(inStr, true, false);
		Test(inStr, true, true);
	}
}
