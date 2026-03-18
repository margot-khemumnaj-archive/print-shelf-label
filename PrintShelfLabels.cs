public PrintResult PrintShelfLabels(PrintInfo pi, List<ShelfLabelItemAndPrice> labelsToPrint) // patterned after PrintCountTag below; only called from RetailPricesControl
{
    BarcodeCE Prce = null;
    mostRecentResult = new PrintResult();
    mostRecentResult.Filename = ""; // No file
    try
    {
        Prce = new BarcodeCE("LICENSE-REDACTED"); // ARCHIVER NOTE: Original contained proprietary license identifier

        double labelWidth = 2.875;
        double labelHeight = 1.0;
        double labelCenterX = 2.875 / 2.0;

        Prce.PrOrientation = PrinterCE_Base.ORIENTATION.PORTRAIT;

        Prce.ScaleMode = PrinterCE_Base.MEASUREMENT_UNITS.INCHES;
        InitializePrinterSettings(pi.Config, Prce);

        Prce.SetupPaperCustom(PrinterCE_Base.ORIENTATION.PORTRAIT, 3.125, 1.125);
        Prce.SetupPrinterOther(
            PrinterCE_Base.FORMFEED_SETTING.NORMAL,
            0.0,
            PrinterCE_Base.DENSITY.USE_CURRENT,
            PrinterCE_Base.SERIAL_HANDSHAKE.USE_CURRENT,
            PrinterCE_Base.BITFLAG.USE_CURRENT,
            PrinterCE_Base.COMPRESSION.USE_CURRENT,
            PrinterCE_Base.DITHER.USE_CURRENT,
            PrinterCE_Base.PRINTQUALITY.USE_CURRENT);

        Prce.PgIndentLeft = 0;
        Prce.PgIndentTop = 0;

        Prce.PrLeftMargin = 0.0;
        Prce.PrRightMargin = 0.0;

        Prce.PrTopMargin = 0.0;
        Prce.PrBottomMargin = 0.0;

        double xIndent = 0.2;

        var Cache = pi.Cache;

        foreach (ShelfLabelItemAndPrice shelfLabelItemAndPrice in labelsToPrint)
        {
            int itemNid = shelfLabelItemAndPrice.ItemNid;
            int brandNid = Cache.MobileDownload.ItemRecord(itemNid).BrandNid;
            int pkgNid = Cache.MobileDownload.ItemRecord(itemNid).PackageNid;

            if (brandNid == 0)
                continue;

            string brandName = Cache.MobileDownload.Brand(brandNid).RecName.Trim().ToUpper();
            string fullDescription = Cache.MobileDownload.ItemRecord(itemNid).FullDescription;

            // no full description?  try to use the package name
            if (fullDescription.Trim() == "" && pkgNid != 0 && Cache.MobileDownload.Package(pkgNid).RecName.Trim() != "")
            {
                fullDescription = Cache.MobileDownload.Package(pkgNid).RecName.Trim();
            }

            if (fullDescription.Trim() == "") // still nada, so use the item name
            {
                fullDescription = Cache.MobileDownload.ItemRecord(itemNid).RecName;
            }

            string retailPrice = ((decimal)shelfLabelItemAndPrice.RetailPrice).ToString("0.00");

            // brand name varies in size and number of "words"; need to select proper font for fitting nicely
            // in right half of the label

            string[] words = brandName.Split(' ');

            // no more than 4 lines, and no more than 4 words supported ... there are 11 possible configurations

            string line1 = "";
            string line2 = "";
            string line3 = "";
            string line4 = "";

            string word1= "";
            string word2= "";
            string word3= "";
            string word4= "";

            int nWords = 0;

            if (words.Length >= 1 && words[0].Trim() != "")
            {
                word1 = words[0].Trim(); ++nWords;
            }
            if (words.Length >= 2 && words[1].Trim() != "")
            {
                word2 = words[1].Trim(); ++nWords;
            }
            if (words.Length >= 3 && words[2].Trim() != "")
            {
                word3 = words[2].Trim(); ++nWords;
            }
            if (words.Length >= 4 && words[3].Trim() != "")
            {
                word4 = words[3].Trim(); ++nWords;
            }

            int linesUsed= 0;
            double maxWidth = labelCenterX - 0.2;

            // if there is just one word, I find the largest font that will fit it on a single line;  if there are 2 words I find the largest
            // font (same for both words) that will fit them on 2 lines; otherwise
            // I go to some trouble to arrange the (up to 4 words on 4 lines) stuff
            // nicely.

            if (nWords == 1)
            {
                Prce.FontBold = true;
                Prce.FontBoldVal = 700;

                for (int fontsize = 32; fontsize >= 10; --fontsize)
                {
                    Prce.FontSize = fontsize;
                    if (Prce.GetStringWidth(word1) <= maxWidth)
                    {
                        line1 = word1;
                        linesUsed = 1;
                        break;
                    }
                }
            }
            else
            if (nWords == 2)
            {
                Prce.FontBold = true;
                Prce.FontBoldVal = 700;

                for (int fontsize = 24; fontsize >= 10; --fontsize)
                {
                    Prce.FontSize = fontsize;
                    if (Prce.GetStringWidth(word1) <= maxWidth && Prce.GetStringWidth(word2) <= maxWidth)
                    {
                        line1 = word1;
                        line2 = word2;
                        linesUsed = 2;
                        break;
                    }
                }
            }
            else
            if (nWords == 3)
            {
                Prce.FontSize = 16;
                Prce.FontBold = true;
                Prce.FontBoldVal = 700;

                double word1word2word3_w = Prce.GetStringWidth(word1 + " " + word2 + " " + word3);
                double word1word2_w = Prce.GetStringWidth(word1 + " " + word2);
                double word2word3_w = Prce.GetStringWidth(word2 + " " + word3);

                string word1word2word3 = word1 + " " + word2 + " " + word3;
                string word1word2 = word1 + " " + word2;
                string word2word3 = word2 + " " + word3;

                if (word1word2word3_w <= maxWidth)
                {
                    line1 = word1word2word3; linesUsed = 1; // all words first line
                }
                else
                if (word1word2_w <= maxWidth)
                {
                    line1 = word1word2; line2 = word3; linesUsed = 2; // first 2 words on first line, 3rd word on second line
                }
                else
                if (word2word3_w <= maxWidth)
                {
                    line1 = word1; line2 = word2word3; linesUsed = 2; // only 1 word fit on first line, remaining 2 fit on next line
                }
                else
                {
                    line1 = word1; line2 = word2; line3 = word3; linesUsed = 3; // needed 1 word per line
                }
            }
            else
            if (nWords == 4)
            {
                Prce.FontSize = 16;
                Prce.FontBold = true;
                Prce.FontBoldVal = 700;

                double word1word2word3word4_w = Prce.GetStringWidth(word1 + " " + word2 + " " + word3 + " " + word4);
                double word1word2word3_w = Prce.GetStringWidth(word1 + " " + word2 + " " + word3);
                double word2word3word4_w = Prce.GetStringWidth(word2 + " " + word3 + " " + word4);
                double word1word2_w = Prce.GetStringWidth(word1 + " " + word2);
                double word2word3_w = Prce.GetStringWidth(word2 + " " + word3);
                double word3word4_w = Prce.GetStringWidth(word3 + " " + word4);

                string word1word2word3word4 = word1 + " " + word2 + " " + word3 + " " + word4;
                string word1word2word3 = word1 + " " + word2 + " " + word3;
                string word2word3word4 = word2 + " " + word3 + " " + word4;
                string word1word2 = word1 + " " + word2;
                string word2word3 = word2 + " " + word3;
                string word3word4 = word3 + " " + word4;

                if (word1word2word3word4_w <= maxWidth)
                {
                    line1 = word1word2word3word4; linesUsed = 1;
                }
                else
                if (word1word2word3_w <= maxWidth)
                {
                    line1 = word1word2word3; line2 = word4;  linesUsed = 2;
                }
                else
                if (word1word2_w <= maxWidth)
                {
                    line1 = word1word2;
                    if (word3word4_w <= maxWidth)
                    {
                        line2 = word3word4; linesUsed = 2;
                    }
                    else
                    {
                        line2 = word3; line3 = word4; linesUsed = 3;
                    }
                }
                else // first word on first line ... fit rest on remaining lines, just like trying to fit 3 words on 3 lines above
                {
                    line1 = word1;

                    if (word2word3word4_w <= maxWidth)
                    {
                        line2 = word2word3word4; linesUsed = 2;
                    }
                    else
                    if (word2word3_w <= maxWidth)
                    {
                        line2 = word2word3; line3 = word4; linesUsed = 3;
                    }
                    else
                    if (word3word4_w <= maxWidth)
                    {
                        line2 = word2; line3 = word3word4; linesUsed = 3;
                    }
                    else
                    {
                        line2 = word2; line3 = word3; line4 = word4; linesUsed = 4;
                    }
                }
            }


            double heightOfAllBrandNameLines = linesUsed * Prce.GetStringHeight;
            double yOffset = (labelHeight / 2.0) - (heightOfAllBrandNameLines / 2.0) - 0.1;
            double xOffset = 0.0;
            switch (linesUsed)
            {
                case 1:
                    xOffset = (labelCenterX / 2.0) - (Prce.GetStringWidth(line1) / 2.0);
                    Prce.DrawText(line1, xOffset + xIndent, yOffset);
                    break;
                case 2:
                    xOffset = (labelCenterX / 2.0) - (Prce.GetStringWidth(line1) / 2.0);
                    Prce.DrawText(line1, xOffset + xIndent, (yOffset - 0.05));
                    yOffset += Prce.GetStringHeight;
                    xOffset = (labelCenterX / 2.0) - (Prce.GetStringWidth(line2) / 2.0);
                    Prce.DrawText(line2, xOffset + xIndent, (yOffset - 0.05));
                    break;
                case 3:
                    xOffset = (labelCenterX / 2.0) - (Prce.GetStringWidth(line1) / 2.0);
                    Prce.DrawText(line1, xOffset + xIndent, (yOffset - 0.05));
                    yOffset += Prce.GetStringHeight;
                    xOffset = (labelCenterX / 2.0) - (Prce.GetStringWidth(line2) / 2.0);
                    Prce.DrawText(line2, xOffset + xIndent, (yOffset - 0.05));
                    yOffset += Prce.GetStringHeight;
                    xOffset = (labelCenterX / 2.0) - (Prce.GetStringWidth(line3) / 2.0);
                    Prce.DrawText(line3, xOffset + xIndent, (yOffset - 0.05));
                    break;
                case 4:
                    xOffset = (labelCenterX / 2.0) - (Prce.GetStringWidth(line1) / 2.0);
                    Prce.DrawText(line1, xOffset + xIndent, (yOffset - 0.05));
                    yOffset += Prce.GetStringHeight;
                    xOffset = (labelCenterX / 2.0) - (Prce.GetStringWidth(line2) / 2.0);
                    Prce.DrawText(line2, xOffset + xIndent, (yOffset - 0.05));
                    yOffset += Prce.GetStringHeight;
                    xOffset = (labelCenterX / 2.0) - (Prce.GetStringWidth(line3) / 2.0);
                    Prce.DrawText(line3, xOffset + xIndent, (yOffset - 0.05));
                    yOffset += Prce.GetStringHeight;
                    xOffset = (labelCenterX / 2.0) - (Prce.GetStringWidth(line4) / 2.0);
                    Prce.DrawText(line4, xOffset + xIndent, (yOffset - 0.05));
                    break;
            }

            // print barcode for item above price

            string barcode= Cache.GetBarcode(itemNid, "");
            if (barcode != "")
                Prce.DrawCode128(barcode, labelCenterX + 0.25 + xIndent, 0.04, BarcodeCE.BARCODE_FONTSIZE.NO_TEXT, BarcodeCE.TYPE_128.AUTO, BarcodeCE.BARCODE_FIT.TIGHTminus1, 25);

            // price is centered in right half of label

            Prce.FontSize = 32;
            Prce.FontBold = true;
            Prce.FontBoldVal = 700;

            string priceString= retailPrice;
            double priceWidth = Prce.GetStringWidth(priceString);
            double priceHeight = Prce.GetStringHeight;

            double priceX = (labelCenterX + labelCenterX / 2.0) - (priceWidth / 2.0);
            double priceY = (labelHeight / 2.0) - (priceHeight / 2.0) - 0.0625;

            Prce.DrawText(priceString, priceX + xIndent, priceY);

            // a smaller "$" is printed to left of the price, lined up with top

            Prce.FontSize = 16;
            Prce.FontBold = true;
            Prce.FontBoldVal = 700;

            string dollarString = "$";
            double dollarWidth = Prce.GetStringWidth(dollarString);
            double dollarHeight = Prce.GetStringHeight;

            Prce.DrawText(dollarString, (priceX - dollarWidth - 0.05) + xIndent, priceY + 0.04);

            // full description is centered in right half of label just above the bottom ... they need to make sure the full descriptions are "short" 'nuff
            // ... however, if it doesn't fit in that area, then I run it across the bottom of the label (right justified) in largest font that will fit

            Prce.FontSize = 12;
            Prce.FontBold = true;
            Prce.FontBoldVal = 500;

            string fullDescriptionString = fullDescription;
            double fullDescriptionWidth = Prce.GetStringWidth(fullDescriptionString);
            double fullDescriptionHeight = Prce.GetStringHeight;

            // KJQ 1/13/10 I just fit it across the bottom of the label (right justified) ...
            // and always in the same font, hence replaced commented out code with the 3 lines below

            double fullDescriptionX = (labelWidth - 0.1) - Prce.GetStringWidth(fullDescriptionString);
            double fullDescriptionY = (labelHeight) - (Prce.GetStringHeight) - 0.1;

            Prce.DrawText(fullDescriptionString, fullDescriptionX + xIndent, fullDescriptionY);

            Prce.NewPage();
        }
    }
    catch (PrinterCEException ex)
    {
        mostRecentResult.Result = false;
        mostRecentResult.Message = ex.Message;
        mostRecentResult.Filename = "";
    }
    finally
    {
        FinalizePrinting(pi, Prce);
    }

    mostRecentResult.Result = true;
    return mostRecentResult;
}
