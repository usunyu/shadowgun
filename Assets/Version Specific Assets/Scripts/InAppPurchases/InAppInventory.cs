//
// By using or accessing the source codes or any other information of the Game SHADOWGUN: DeadZone ("Game"),
// you ("You" or "Licensee") agree to be bound by all the terms and conditions of SHADOWGUN: DeadZone Public
// License Agreement (the "PLA") starting the day you access the "Game" under the Terms of the "PLA".
//
// You can review the most current version of the "PLA" at any time at: http://madfingergames.com/pla/deadzone
//
// If you don't agree to all the terms and conditions of the "PLA", you shouldn't, and aren't permitted
// to use or access the source codes or any other information of the "Game" supplied by MADFINGER Games, a.s.
//

using System.Collections;
using System.Collections.Generic;

public class InAppInventory : IEnumerable
{
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	protected Dictionary<string, InAppProduct> m_Products = new Dictionary<string, InAppProduct>();

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	internal void AddProduct(InAppProduct product)
	{
		m_Products[product.ProductId] = product;
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	internal void RemoveProduct(InAppProduct product)
	{
		m_Products.Remove(product.ProductId);
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	public bool IsProductAvailable(string productId)
	{
		return m_Products.ContainsKey(productId);
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	public InAppProduct Product(string productId)
	{
		InAppProduct p;

		return m_Products.TryGetValue(productId, out p) ? p : null;
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	public IEnumerator GetEnumerator()
	{
		return m_Products.Values.GetEnumerator();
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
	// Debug methods
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
/*	
	public string DebugProductsIdsString()
	{
		string productsIds = "";
		
		foreach (InAppProduct product in m_Products.Values)
		{
			productsIds = productsIds + product.ProductId + " price:" + product.Price + " currency:" + product.CurrencyCode + "\n";
		}
		
		return productsIds;
	}
*/

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
}
