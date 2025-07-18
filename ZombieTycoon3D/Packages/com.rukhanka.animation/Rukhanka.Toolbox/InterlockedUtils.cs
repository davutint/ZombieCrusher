using System.Threading;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public static class InterlockedExtensions
{
	public static bool InterlockedMax(ref int location, int newValue)
    {
	    int l;
	    do
	    {
		    l = location;
		    if (l >= newValue)
			    return false;
	    }
	    while (Interlocked.CompareExchange(ref location, newValue, l) != l);
	    return true;
    }
}
}
