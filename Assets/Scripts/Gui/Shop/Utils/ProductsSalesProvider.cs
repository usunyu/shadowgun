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

using UnityEngine;
using System.Collections.Generic;
using System;

//--------------------------------------------------------------------------------------------------
//--------------------------------------------------------------------------------------------------

public class ProductsSalesProvider
{
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
	// Public methods
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	// Return the product sale relative to the cheapest product in the inventory.
	// return number < 0 if getting product sale fails
	public static int GetProductSaleInPercent(string productId, InAppInventory productsInventory, FundSettingsManager fundSettingsManager)
	{
		// method failed return code
		const int failed = -1;

		string cheapestProductId = GetCheapestProduct(productsInventory).ProductId;

		if (cheapestProductId != null)
		{
			return GetProductSaleInPercent(productId, cheapestProductId, productsInventory, fundSettingsManager);
		}
		else
		{
			return failed;
		}
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	// Return the product sale relative to the cheapest product in the shop funds.
	// return number < 0 if getting product sale fails
	public static int GetProductSaleInPercent(string productId,
											  List<ShopItemId> shopFunds,
											  InAppInventory productsInventory,
											  FundSettingsManager fundSettingsManager)
	{
		// method failed return code
		const int failed = -1;

		string cheapestProductId = GetCheapestProductId(shopFunds, productsInventory, fundSettingsManager);

		if (cheapestProductId != null)
		{
			return GetProductSaleInPercent(productId, cheapestProductId, productsInventory, fundSettingsManager);
		}
		else
		{
			return failed;
		}
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	// Return the product sale relative to the base product.
	// return number < 0 if getting product sale fails
	public static int GetProductSaleInPercent(string productId,
											  string baseProductId,
											  InAppInventory productsInventory,
											  FundSettingsManager fundSettingsManager)
	{
		// method failed return code
		const int failed = -1;

		InAppProduct product = productsInventory.Product(productId);
		InAppProduct baseProduct = productsInventory.Product(baseProductId);

		if (baseProduct == null)
		{
			Debug.LogWarning("ProductsSalesProvider -> GetProductSaleInPercent: BaseProduct with id " + baseProductId +
							 " is not contained in the inventory. Product sale cannot be computed.");
			return failed;
		}

		if (product == null)
			return failed;

		float baseProductCoinPrice = GetOneGoldCoinPrice(baseProduct, fundSettingsManager);
		float productCoinPrice = GetOneGoldCoinPrice(product, fundSettingsManager);

		float productSale = (baseProductCoinPrice/productCoinPrice) - 1;
		int productSaleInPercent = (int)Math.Round(productSale*100);

		return productSaleInPercent;
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
	// Private methods
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	static float GetOneGoldCoinPrice(InAppProduct product, FundSettingsManager fundSettingsManager)
	{
		float productPrice = GetProductPrice(product);
		int goldCoinsCount = GetGoldCoinsCount(product, fundSettingsManager);

		return productPrice/goldCoinsCount;
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	// return number < 0 if fails
	static int GetGoldCoinsCount(InAppProduct product, FundSettingsManager fundSettingsManager)
	{
		// method failed return code
		const int failed = -1;

		try
		{
			int fundGUID = GetFundGUID(product.ProductId);
			FundSettings fund = fundSettingsManager.FindByGUID(fundGUID);

			return fund.AddGold;
		}
		catch
		{
			return failed;
		}
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	// return null if fails
	static string GetCheapestProductId(List<ShopItemId> shopFunds, InAppInventory productsInventory, FundSettingsManager fundSettingsManager)
	{
		string cheapestProductId = null;
		float cheapestProductPrice = float.MaxValue;

		foreach (ShopItemId shopFund in shopFunds)
		{
			float productPrice = GetProductPriceForFund(shopFund, productsInventory, fundSettingsManager);

			if (productPrice >= 0 && productPrice < cheapestProductPrice)
			{
				cheapestProductId = GetProductId(shopFund, fundSettingsManager);
				cheapestProductPrice = productPrice;
			}
		}

		return cheapestProductId;
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	// return null if fails
	static InAppProduct GetCheapestProduct(InAppInventory productsInventory)
	{
		InAppProduct cheapestProduct = null;
		float cheapestProductPrice = float.MaxValue;

		foreach (InAppProduct product in productsInventory)
		{
			float productPrice = GetProductPrice(product);

			if (productPrice >= 0 && productPrice < cheapestProductPrice)
			{
				cheapestProduct = product;
				cheapestProductPrice = productPrice;
			}
		}

		return cheapestProduct;
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	// return number < 0 if fails
	static float GetProductPriceForFund(ShopItemId shopFund, InAppInventory productsInventory, FundSettingsManager fundSettingsManager)
	{
		// method failed return code
		const float failed = -1;

		string productId = GetProductId(shopFund, fundSettingsManager);

		if (productId == null)
			return failed;

		InAppProduct product = productsInventory.Product(productId);

		if (product == null)
			return failed;

		return GetProductPrice(product);
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	// return number < 0 if fails
	static float GetProductPrice(InAppProduct product)
	{
		// method failed return code
		const float failed = -1;

		try
		{
			return Convert.ToSingle(product.Price);
		}
		catch (Exception e)
		{
			Debug.LogError("ProductsSalesProvider->GetProductPrice\n" + "Product price: " + product.Price + "\n" + "Exception: " + e.ToString());
			return failed;
		}
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	// if fails throw exception
	static int GetFundGUID(string productId)
	{
		try
		{
			return Convert.ToInt32(productId);
		}
		catch (Exception e)
		{
			Debug.LogError("ProductsSalesProvider->GetFundGUID\n" + "productId: " + productId + "\n" + "Exception: " + e.ToString());
			throw (e);
		}
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	// return null if fails
	static string GetProductId(ShopItemId shopItem, FundSettingsManager fundSettingsManager)
	{
		return fundSettingsManager.Get((E_FundID)(shopItem.Id)).GUID.ToString();
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
}
