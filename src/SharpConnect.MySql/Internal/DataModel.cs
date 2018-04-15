//LICENSE: MIT
//Copyright(c) 2012 Felix Geisendörfer(felix @debuggable.com) and contributors 
//MIT, 2015-2018, brezza92, EngineKit and contributors

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

using System;
using System.Collections.Generic;
namespace SharpConnect.MySql.Internal
{
    enum CharSets
    {
        BIG5_CHINESE_CI = 1,//exports.BIG5_CHINESE_CI              = 1;
        LATIN2_CZECH_CS,    //exports.LATIN2_CZECH_CS              = 2;
        DEC8_SWEDISH_CI,    //exports.DEC8_SWEDISH_CI              = 3;
        CP850_GENERAL_CI,   //exports.CP850_GENERAL_CI             = 4;
        LATIN1_GERMAN1_CI,  //exports.LATIN1_GERMAN1_CI            = 5;
        HP8_ENGLISH_CI,     //exports.HP8_ENGLISH_CI               = 6;
        KOI8R_GENERAL_CI,   //exports.KOI8R_GENERAL_CI             = 7;
        LATIN1_SWEDISH_CI,  //exports.LATIN1_SWEDISH_CI            = 8;
        LATIN2_GENERAL_CI,  //exports.LATIN2_GENERAL_CI            = 9;
        SWE7_SWEDISH_CI,    //exports.SWE7_SWEDISH_CI              = 10;
        ASCII_GENERAL_CI,   //exports.ASCII_GENERAL_CI             = 11;
        UJIS_JAPANESE_CI,   //exports.UJIS_JAPANESE_CI             = 12;
        SJIS_JAPANESE_CI,   //exports.SJIS_JAPANESE_CI             = 13;
        CP1251_BULGARIAN_CI,//exports.CP1251_BULGARIAN_CI          = 14;
        LATIN1_DANISH_CI,   //exports.LATIN1_DANISH_CI             = 15;
        HEBREW_GENERAL_CI,  //exports.HEBREW_GENERAL_CI            = 16;
        TIS620_THAI_CI = 18,//exports.TIS620_THAI_CI               = 18;
        EUCKR_KOREAN_CI,    //exports.EUCKR_KOREAN_CI              = 19;
        LATIN7_ESTONIAN_CS, //exports.LATIN7_ESTONIAN_CS           = 20;
        LATIN2_HUNGARIAN_CI,//exports.LATIN2_HUNGARIAN_CI          = 21;
        KOI8U_GENERAL_CI,   //exports.KOI8U_GENERAL_CI             = 22;
        CP1251_UKRAINIAN_CI,//exports.CP1251_UKRAINIAN_CI          = 23;
        GB2312_CHINESE_CI,  //exports.GB2312_CHINESE_CI            = 24;
        GREEK_GENERAL_CI,   //exports.GREEK_GENERAL_CI             = 25;
        CP1250_GENERAL_CI,  //exports.CP1250_GENERAL_CI            = 26;
        LATIN2_CROATIAN_CI, //exports.LATIN2_CROATIAN_CI           = 27;
        GBK_CHINESE_CI,     //exports.GBK_CHINESE_CI               = 28;
        CP1257_LITHUANIAN_CI,//exports.CP1257_LITHUANIAN_CI        = 29;
        LATIN5_TURKISH_CI,  //exports.LATIN5_TURKISH_CI            = 30;
        LATIN1_GERMAN2_CI,  //exports.LATIN1_GERMAN2_CI            = 31;
        ARMSCII8_GENERAL_CI,//exports.ARMSCII8_GENERAL_CI          = 32;
        UTF8_GENERAL_CI,    //exports.UTF8_GENERAL_CI              = 33;
        CP1250_CZECH_CS,    //exports.CP1250_CZECH_CS              = 34;
        UCS2_GENERAL_CI,    //exports.UCS2_GENERAL_CI              = 35;
        CP866_GENERAL_CI,   //exports.CP866_GENERAL_CI             = 36;
        KEYBCS2_GENERAL_CI, //exports.KEYBCS2_GENERAL_CI           = 37;
        MACCE_GENERAL_CI,   //exports.MACCE_GENERAL_CI             = 38;
        MACROMAN_GENERAL_CI,//exports.MACROMAN_GENERAL_CI          = 39;
        CP852_GENERAL_CI,   //exports.CP852_GENERAL_CI             = 40;
        LATIN7_GENERAL_CI,  //exports.LATIN7_GENERAL_CI            = 41;
        LATIN7_GENERAL_CS,  //exports.LATIN7_GENERAL_CS            = 42;
        MACCE_BIN,          //exports.MACCE_BIN                    = 43;
        CP1250_CROATIAN_CI, //exports.CP1250_CROATIAN_CI           = 44;
        UTF8MB4_GENERAL_CI, //exports.UTF8MB4_GENERAL_CI           = 45;
        UTF8MB4_BIN,        //exports.UTF8MB4_BIN                  = 46;
        LATIN1_BIN,         //exports.LATIN1_BIN                   = 47;
        LATIN1_GENERAL_CI,  //exports.LATIN1_GENERAL_CI            = 48;
        LATIN1_GENERAL_CS,  //exports.LATIN1_GENERAL_CS            = 49;
        CP1251_BIN,         //exports.CP1251_BIN                   = 50;
        CP1251_GENERAL_CI,  //exports.CP1251_GENERAL_CI            = 51;
        CP1251_GENERAL_CS,  //exports.CP1251_GENERAL_CS            = 52;
        MACROMAN_BIN,       //exports.MACROMAN_BIN                 = 53;
        UTF16_GENERAL_CI,   //exports.UTF16_GENERAL_CI             = 54;
        UTF16_BIN,          //exports.UTF16_BIN                    = 55;
        UTF16LE_GENERAL_CI, //exports.UTF16LE_GENERAL_CI           = 56;
        CP1256_GENERAL_CI,  //exports.CP1256_GENERAL_CI            = 57;
        CP1257_BIN,         //exports.CP1257_BIN                   = 58;
        CP1257_GENERAL_CI,  //exports.CP1257_GENERAL_CI            = 59;
        UTF32_GENERAL_CI,   //exports.UTF32_GENERAL_CI             = 60;
        UTF32_BIN,          //exports.UTF32_BIN                    = 61;
        UTF16LE_BIN,        //exports.UTF16LE_BIN                  = 62;
        BINARY,             //exports.BINARY                       = 63;
        ARMSCII8_BIN,       //exports.ARMSCII8_BIN                 = 64;
        ASCII_BIN,          //exports.ASCII_BIN                    = 65;
        CP1250_BIN,         //exports.CP1250_BIN                   = 66;
        CP1256_BIN,         //exports.CP1256_BIN                   = 67;
        CP866_BIN,          //exports.CP866_BIN                    = 68;
        DEC8_BIN,           //exports.DEC8_BIN                     = 69;
        GREEK_BIN,          //exports.GREEK_BIN                    = 70;
        HEBREW_BIN,         //exports.HEBREW_BIN                   = 71;
        HP8_BIN,            //exports.HP8_BIN                      = 72;
        KEYBCS2_BIN,        //exports.KEYBCS2_BIN                  = 73;
        KOI8R_BIN,          //exports.KOI8R_BIN                    = 74;
        KOI8U_BIN,          //exports.KOI8U_BIN                    = 75;
        LATIN2_BIN = 77,    //exports.LATIN2_BIN                   = 77;
        LATIN5_BIN,         //exports.LATIN5_BIN                   = 78;
        LATIN7_BIN,         //exports.LATIN7_BIN                   = 79;
        CP850_BIN,          //exports.CP850_BIN                    = 80;
        CP852_BIN,          //exports.CP852_BIN                    = 81;
        SWE7_BIN,           //exports.SWE7_BIN                     = 82;
        UTF8_BIN,           //exports.UTF8_BIN                     = 83;
        BIG5_BIN,           //exports.BIG5_BIN                     = 84;
        EUCKR_BIN,          //exports.EUCKR_BIN                    = 85;
        GB2312_BIN,         //exports.GB2312_BIN                   = 86;
        GBK_BIN,            //exports.GBK_BIN                      = 87;
        SJIS_BIN,           //exports.SJIS_BIN                     = 88;
        TIS620_BIN,         //exports.TIS620_BIN                   = 89;
        UCS2_BIN,           //exports.UCS2_BIN                     = 90;
        UJIS_BIN,           //exports.UJIS_BIN                     = 91;
        GEOSTD8_GENERAL_CI, //exports.GEOSTD8_GENERAL_CI           = 92;
        GEOSTD8_BIN,        //exports.GEOSTD8_BIN                  = 93;
        LATIN1_SPANISH_CI,  //exports.LATIN1_SPANISH_CI            = 94;
        CP932_JAPANESE_CI,  //exports.CP932_JAPANESE_CI            = 95;
        CP932_BIN,          //exports.CP932_BIN                    = 96;
        EUCJPMS_JAPANESE_CI,//exports.EUCJPMS_JAPANESE_CI          = 97;
        EUCJPMS_BIN,        //exports.EUCJPMS_BIN                  = 98;
        CP1250_POLISH_CI,   //exports.CP1250_POLISH_CI             = 99;
        UTF16_UNICODE_CI = 101,//exports.UTF16_UNICODE_CI          = 101;
        UTF16_ICELANDIC_CI, //exports.UTF16_ICELANDIC_CI           = 102;
        UTF16_LATVIAN_CI,   //exports.UTF16_LATVIAN_CI             = 103;
        UTF16_ROMANIAN_CI,  //exports.UTF16_ROMANIAN_CI            = 104;
        UTF16_SLOVENIAN_CI, //exports.UTF16_SLOVENIAN_CI           = 105;
        UTF16_POLISH_CI,    //exports.UTF16_POLISH_CI              = 106;
        UTF16_ESTONIAN_CI,  //exports.UTF16_ESTONIAN_CI            = 107;
        UTF16_SPANISH_CI,   //exports.UTF16_SPANISH_CI             = 108;
        UTF16_SWEDISH_CI,   //exports.UTF16_SWEDISH_CI             = 109;
        UTF16_TURKISH_CI,   //exports.UTF16_TURKISH_CI             = 110;
        UTF16_CZECH_CI,     //exports.UTF16_CZECH_CI               = 111;
        UTF16_DANISH_CI,    //exports.UTF16_DANISH_CI              = 112;
        UTF16_LITHUANIAN_CI,//exports.UTF16_LITHUANIAN_CI          = 113;
        UTF16_SLOVAK_CI,    //exports.UTF16_SLOVAK_CI              = 114;
        UTF16_SPANISH2_CI,  //exports.UTF16_SPANISH2_CI            = 115;
        UTF16_ROMAN_CI,     //exports.UTF16_ROMAN_CI               = 116;
        UTF16_PERSIAN_CI,   //exports.UTF16_PERSIAN_CI             = 117;
        UTF16_ESPERANTO_CI, //exports.UTF16_ESPERANTO_CI           = 118;
        UTF16_HUNGARIAN_CI, //exports.UTF16_HUNGARIAN_CI           = 119;
        UTF16_SINHALA_CI,   //exports.UTF16_SINHALA_CI             = 120;
        UTF16_GERMAN2_CI,   //exports.UTF16_GERMAN2_CI             = 121;
        UTF16_CROATIAN_MYSQL561_CI,//exports.UTF16_CROATIAN_MYSQL561_CI = 122;
        UTF16_UNICODE_520_CI,//exports.UTF16_UNICODE_520_CI        = 123;
        UTF16_VIETNAMESE_CI,//exports.UTF16_VIETNAMESE_CI          = 124;
        UCS2_UNICODE_CI = 128,//exports.UCS2_UNICODE_CI            = 128;
        UCS2_ICELANDIC_CI,  //exports.UCS2_ICELANDIC_CI            = 129;
        UCS2_LATVIAN_CI,    //exports.UCS2_LATVIAN_CI              = 130;
        UCS2_ROMANIAN_CI,   //exports.UCS2_ROMANIAN_CI             = 131;
        UCS2_SLOVENIAN_CI,  //exports.UCS2_SLOVENIAN_CI            = 132;
        UCS2_POLISH_CI,     //exports.UCS2_POLISH_CI               = 133;
        UCS2_ESTONIAN_CI,   //exports.UCS2_ESTONIAN_CI             = 134;
        UCS2_SPANISH_CI,    //exports.UCS2_SPANISH_CI              = 135;
        UCS2_SWEDISH_CI,    //exports.UCS2_SWEDISH_CI              = 136;
        UCS2_TURKISH_CI,    //exports.UCS2_TURKISH_CI              = 137;
        UCS2_CZECH_CI,      //exports.UCS2_CZECH_CI                = 138;
        UCS2_DANISH_CI,     //exports.UCS2_DANISH_CI               = 139;
        UCS2_LITHUANIAN_CI, //exports.UCS2_LITHUANIAN_CI           = 140;
        UCS2_SLOVAK_CI,     //exports.UCS2_SLOVAK_CI               = 141;
        UCS2_SPANISH2_CI,   //exports.UCS2_SPANISH2_CI             = 142;
        UCS2_ROMAN_CI,      //exports.UCS2_ROMAN_CI                = 143;
        UCS2_PERSIAN_CI,    //exports.UCS2_PERSIAN_CI              = 144;
        UCS2_ESPERANTO_CI,  //exports.UCS2_ESPERANTO_CI            = 145;
        UCS2_HUNGARIAN_CI,  //exports.UCS2_HUNGARIAN_CI            = 146;
        UCS2_SINHALA_CI,    //exports.UCS2_SINHALA_CI              = 147;
        UCS2_GERMAN2_CI,    //exports.UCS2_GERMAN2_CI              = 148;
        UCS2_CROATIAN_MYSQL561_CI,//exports.UCS2_CROATIAN_MYSQL561_CI = 149;
        UCS2_UNICODE_520_CI,//exports.UCS2_UNICODE_520_CI          = 150;
        UCS2_VIETNAMESE_CI, //exports.UCS2_VIETNAMESE_CI           = 151;
        UCS2_GENERAL_MYSQL500_CI = 159,//exports.UCS2_GENERAL_MYSQL500_CI= 159;
        UTF32_UNICODE_CI,   //exports.UTF32_UNICODE_CI             = 160;
        UTF32_ICELANDIC_CI, //exports.UTF32_ICELANDIC_CI           = 161;
        UTF32_LATVIAN_CI,   //exports.UTF32_LATVIAN_CI             = 162;
        UTF32_ROMANIAN_CI,  //exports.UTF32_ROMANIAN_CI            = 163;
        UTF32_SLOVENIAN_CI, //exports.UTF32_SLOVENIAN_CI           = 164;
        UTF32_POLISH_CI,    //exports.UTF32_POLISH_CI              = 165;
        UTF32_ESTONIAN_CI,  //exports.UTF32_ESTONIAN_CI            = 166;
        UTF32_SPANISH_CI,   //exports.UTF32_SPANISH_CI             = 167;
        UTF32_SWEDISH_CI,   //exports.UTF32_SWEDISH_CI             = 168;
        UTF32_TURKISH_CI,   //exports.UTF32_TURKISH_CI             = 169;
        UTF32_CZECH_CI,     //exports.UTF32_CZECH_CI               = 170;
        UTF32_DANISH_CI,    //exports.UTF32_DANISH_CI              = 171;
        UTF32_LITHUANIAN_CI,//exports.UTF32_LITHUANIAN_CI          = 172;
        UTF32_SLOVAK_CI,    //exports.UTF32_SLOVAK_CI              = 173;
        UTF32_SPANISH2_CI,  //exports.UTF32_SPANISH2_CI            = 174;
        UTF32_ROMAN_CI,     //exports.UTF32_ROMAN_CI               = 175;
        UTF32_PERSIAN_CI,   //exports.UTF32_PERSIAN_CI             = 176;
        UTF32_ESPERANTO_CI, //exports.UTF32_ESPERANTO_CI           = 177;
        UTF32_HUNGARIAN_CI, //exports.UTF32_HUNGARIAN_CI           = 178;
        UTF32_SINHALA_CI,   //exports.UTF32_SINHALA_CI             = 179;
        UTF32_GERMAN2_CI,   //exports.UTF32_GERMAN2_CI             = 180;
        UTF32_CROATIAN_MYSQL561_CI,//exports.UTF32_CROATIAN_MYSQL561_CI= 181;
        UTF32_UNICODE_520_CI,//exports.UTF32_UNICODE_520_CI        = 182;
        UTF32_VIETNAMESE_CI,//exports.UTF32_VIETNAMESE_CI          = 183;
        UTF8_UNICODE_CI = 192,    //exports.UTF8_UNICODE_CI        = 192;
        UTF8_ICELANDIC_CI,  //exports.UTF8_ICELANDIC_CI            = 193;
        UTF8_LATVIAN_CI,    //exports.UTF8_LATVIAN_CI              = 194;
        UTF8_ROMANIAN_CI,   //exports.UTF8_ROMANIAN_CI             = 195;
        UTF8_SLOVENIAN_CI,  //exports.UTF8_SLOVENIAN_CI            = 196;
        UTF8_POLISH_CI,     //exports.UTF8_POLISH_CI               = 197;
        UTF8_ESTONIAN_CI,   //exports.UTF8_ESTONIAN_CI             = 198;
        UTF8_SPANISH_CI,    //exports.UTF8_SPANISH_CI              = 199;
        UTF8_SWEDISH_CI,    //exports.UTF8_SWEDISH_CI              = 200;
        UTF8_TURKISH_CI,    //exports.UTF8_TURKISH_CI              = 201;
        UTF8_CZECH_CI,      //exports.UTF8_CZECH_CI                = 202;
        UTF8_DANISH_CI,     //exports.UTF8_DANISH_CI               = 203;
        UTF8_LITHUANIAN_CI, //exports.UTF8_LITHUANIAN_CI           = 204;
        UTF8_SLOVAK_CI,     //exports.UTF8_SLOVAK_CI               = 205;
        UTF8_SPANISH2_CI,   //exports.UTF8_SPANISH2_CI             = 206;
        UTF8_ROMAN_CI,      //exports.UTF8_ROMAN_CI                = 207;
        UTF8_PERSIAN_CI,    //exports.UTF8_PERSIAN_CI              = 208;
        UTF8_ESPERANTO_CI,  //exports.UTF8_ESPERANTO_CI            = 209;
        UTF8_HUNGARIAN_CI,  //exports.UTF8_HUNGARIAN_CI            = 210;
        UTF8_SINHALA_CI,    //exports.UTF8_SINHALA_CI              = 211;
        UTF8_GERMAN2_CI,    //exports.UTF8_GERMAN2_CI              = 212;
        UTF8_CROATIAN_MYSQL561_CI,//exports.UTF8_CROATIAN_MYSQL561_CI = 213;
        UTF8_UNICODE_520_CI,//exports.UTF8_UNICODE_520_CI          = 214;
        UTF8_VIETNAMESE_CI, //exports.UTF8_VIETNAMESE_CI           = 215;
        UTF8_GENERAL_MYSQL500_CI = 223,//exports.UTF8_GENERAL_MYSQL500_CI = 223;
        UTF8MB4_UNICODE_CI, //exports.UTF8MB4_UNICODE_CI           = 224;
        UTF8MB4_ICELANDIC_CI,//exports.UTF8MB4_ICELANDIC_CI        = 225;
        UTF8MB4_LATVIAN_CI, //exports.UTF8MB4_LATVIAN_CI           = 226;
        UTF8MB4_ROMANIAN_CI,//exports.UTF8MB4_ROMANIAN_CI          = 227;
        UTF8MB4_SLOVENIAN_CI,//exports.UTF8MB4_SLOVENIAN_CI        = 228;
        UTF8MB4_POLISH_CI,  //exports.UTF8MB4_POLISH_CI            = 229;
        UTF8MB4_ESTONIAN_CI,//exports.UTF8MB4_ESTONIAN_CI          = 230;
        UTF8MB4_SPANISH_CI, //exports.UTF8MB4_SPANISH_CI           = 231;
        UTF8MB4_SWEDISH_CI, //exports.UTF8MB4_SWEDISH_CI           = 232;
        UTF8MB4_TURKISH_CI, //exports.UTF8MB4_TURKISH_CI           = 233;
        UTF8MB4_CZECH_CI,   //exports.UTF8MB4_CZECH_CI             = 234;
        UTF8MB4_DANISH_CI,  //exports.UTF8MB4_DANISH_CI            = 235;
        UTF8MB4_LITHUANIAN_CI,//exports.UTF8MB4_LITHUANIAN_CI      = 236;
        UTF8MB4_SLOVAK_CI,  //exports.UTF8MB4_SLOVAK_CI            = 237;
        UTF8MB4_SPANISH2_CI,//exports.UTF8MB4_SPANISH2_CI          = 238;
        UTF8MB4_ROMAN_CI,   //exports.UTF8MB4_ROMAN_CI             = 239;
        UTF8MB4_PERSIAN_CI, //exports.UTF8MB4_PERSIAN_CI           = 240;
        UTF8MB4_ESPERANTO_CI,//exports.UTF8MB4_ESPERANTO_CI        = 241;
        UTF8MB4_HUNGARIAN_CI,//exports.UTF8MB4_HUNGARIAN_CI        = 242;
        UTF8MB4_SINHALA_CI, //exports.UTF8MB4_SINHALA_CI           = 243;
        UTF8MB4_GERMAN2_CI, //exports.UTF8MB4_GERMAN2_CI           = 244;
        UTF8MB4_CROATIAN_MYSQL561_CI,//exports.UTF8MB4_CROATIAN_MYSQL561_CI = 245;
        UTF8MB4_UNICODE_520_CI,//exports.UTF8MB4_UNICODE_520_CI    = 246;
        UTF8MB4_VIETNAMESE_CI,//exports.UTF8MB4_VIETNAMESE_CI      = 247;
        UTF8_GENERAL50_CI = 253,  //exports.UTF8_GENERAL50_CI      = 253;
        //// short aliases
        ARMSCII8 = ARMSCII8_GENERAL_CI, //exports.ARMSCII8 = exports.ARMSCII8_GENERAL_CI;
        ASCII = ASCII_GENERAL_CI,       //exports.ASCII    = exports.ASCII_GENERAL_CI;
        BIG5 = BIG5_CHINESE_CI,         //exports.BIG5     = exports.BIG5_CHINESE_CI;
        //exports.BINARY   = exports.BINARY;
        CP1250 = CP1250_GENERAL_CI,     //exports.CP1250   = exports.CP1250_GENERAL_CI;
        CP1251 = CP1251_GENERAL_CI,     //exports.CP1251   = exports.CP1251_GENERAL_CI;
        CP1256 = CP1256_GENERAL_CI,     //exports.CP1256   = exports.CP1256_GENERAL_CI;
        CP1257 = CP1257_GENERAL_CI,     //exports.CP1257   = exports.CP1257_GENERAL_CI;
        CP866 = CP866_GENERAL_CI,       //exports.CP866    = exports.CP866_GENERAL_CI;
        CP850 = CP850_GENERAL_CI,       //exports.CP850    = exports.CP850_GENERAL_CI;
        CP852 = CP852_GENERAL_CI,       //exports.CP852    = exports.CP852_GENERAL_CI;
        CP932 = CP932_JAPANESE_CI,      //exports.CP932    = exports.CP932_JAPANESE_CI;
        DEC8 = DEC8_SWEDISH_CI,         //exports.DEC8     = exports.DEC8_SWEDISH_CI;
        EUCJPMS = EUCJPMS_JAPANESE_CI,  //exports.EUCJPMS  = exports.EUCJPMS_JAPANESE_CI;
        EUCKR = EUCKR_KOREAN_CI,        //exports.EUCKR    = exports.EUCKR_KOREAN_CI;
        GB2312 = GB2312_CHINESE_CI,     //exports.GB2312   = exports.GB2312_CHINESE_CI;
        GBK = GBK_CHINESE_CI,           //exports.GBK      = exports.GBK_CHINESE_CI;
        GEOSTD8 = GEOSTD8_GENERAL_CI,   //exports.GEOSTD8  = exports.GEOSTD8_GENERAL_CI;
        GREEK = GREEK_GENERAL_CI,       //exports.GREEK    = exports.GREEK_GENERAL_CI;
        HEBREW = HEBREW_GENERAL_CI,     //exports.HEBREW   = exports.HEBREW_GENERAL_CI;
        HP8 = HP8_ENGLISH_CI,           //exports.HP8      = exports.HP8_ENGLISH_CI;
        KEYBCS2 = KEYBCS2_GENERAL_CI,   //exports.KEYBCS2  = exports.KEYBCS2_GENERAL_CI;
        KOI8R = KOI8R_GENERAL_CI,       //exports.KOI8R    = exports.KOI8R_GENERAL_CI;
        KOI8U = KOI8U_GENERAL_CI,       //exports.KOI8U    = exports.KOI8U_GENERAL_CI;
        LATIN1 = LATIN1_SWEDISH_CI,     //exports.LATIN1   = exports.LATIN1_SWEDISH_CI;
        LATIN2 = LATIN2_GENERAL_CI,     //exports.LATIN2   = exports.LATIN2_GENERAL_CI;
        LATIN5 = LATIN5_TURKISH_CI,     //exports.LATIN5   = exports.LATIN5_TURKISH_CI;
        LATIN7 = LATIN7_GENERAL_CI,     //exports.LATIN7   = exports.LATIN7_GENERAL_CI;
        MACCE = MACCE_GENERAL_CI,       //exports.MACCE    = exports.MACCE_GENERAL_CI;
        MACROMAN = MACROMAN_GENERAL_CI, //exports.MACROMAN = exports.MACROMAN_GENERAL_CI;
        SJIS = SJIS_JAPANESE_CI,        //exports.SJIS     = exports.SJIS_JAPANESE_CI;
        SWE7 = SWE7_SWEDISH_CI,         //exports.SWE7     = exports.SWE7_SWEDISH_CI;
        TIS620 = TIS620_THAI_CI,        //exports.TIS620   = exports.TIS620_THAI_CI;
        UCS2 = UCS2_GENERAL_CI,         //exports.UCS2     = exports.UCS2_GENERAL_CI;
        UJIS = UJIS_JAPANESE_CI,        //exports.UJIS     = exports.UJIS_JAPANESE_CI;
        UTF16 = UTF16_GENERAL_CI,       //exports.UTF16    = exports.UTF16_GENERAL_CI;
        UTF16LE = UTF16LE_GENERAL_CI,   //exports.UTF16LE  = exports.UTF16LE_GENERAL_CI;
        UTF8 = UTF8_GENERAL_CI,         //exports.UTF8     = exports.UTF8_GENERAL_CI;
        UTF8MB4 = UTF8MB4_GENERAL_CI,   //exports.UTF8MB4  = exports.UTF8MB4_GENERAL_CI;
        UTF32 = UTF32_GENERAL_CI        //exports.UTF32    = exports.UTF32_GENERAL_CI;
    }

    /*
    https://dev.mysql.com/doc/internals/en/integer.html#fixed-length-integer
    14.1) Integer Types
        14.1.1) Fixed Length, int<1>, int<2>,int<3>,int<4>, int<6>,int<8>
        14.1.2) Length-Encoded Integer Type
    14.2) String Types
        14.2.1) FixedLengthString
        14.2.2) NulTerminatedString
        14.2.3) VariableLengthString
        14.2.4) LengthEncodedString 
    */


    enum MySqlDataType : byte
    {
        // Manually extracted from mysql-5.5.23/include/mysql_com.h
        // some more info here: http://dev.mysql.com/doc/refman/5.5/en/c-api-prepared-statement-type-codes.html
        DECIMAL = 0x00, //exports.DECIMAL     = 0x00; // aka DECIMAL (http://dev.mysql.com/doc/refman/5.0/en/precision-math-decimal-changes.html)
        /// <summary>
        /// 1 byte (signed value) (-128 to 127), if unsigned => 0-255
        /// </summary>
        TINY,           //exports.TINY        = 0x01; // aka TINYINT, 1 byte
        /// <summary>
        /// 2 bytes
        /// </summary>
        SHORT,          //exports.SHORT       = 0x02; // aka SMALLINT, 2 bytes
        /// <summary>
        /// 4 bytes
        /// </summary>
        LONG,           //exports.LONG        = 0x03; // aka INT, 4 bytes
        /// <summary>
        /// 4-8 bytes
        /// </summary>
        FLOAT,          //exports.FLOAT       = 0x04; // aka FLOAT, 4-8 bytes
        /// <summary>
        /// 8 bytes
        /// </summary>
        DOUBLE,         //exports.DOUBLE      = 0x05; // aka DOUBLE, 8 bytes
        NULL,           //exports.NULL        = 0x06; // NULL (used for prepared statements, I think)
        TIMESTAMP,      //exports.TIMESTAMP   = 0x07; // aka TIMESTAMP
        /// <summary>
        /// 8 bytes
        /// </summary>
        LONGLONG,       //exports.LONGLONG    = 0x08; // aka BIGINT, 8 bytes
        /// <summary>
        /// 3 bytes
        /// </summary>
        INT24,          //exports.INT24       = 0x09; // aka MEDIUMINT, 3 bytes
        DATE,           //exports.DATE        = 0x0a; // aka DATE
        TIME,           //exports.TIME        = 0x0b; // aka TIME
        DATETIME,       //exports.DATETIME    = 0x0c; // aka DATETIME
        YEAR,           //exports.YEAR        = 0x0d; // aka YEAR, 1 byte (don't ask)
        NEWDATE,        //exports.NEWDATE     = 0x0e; // aka ?
        VARCHAR,        //exports.VARCHAR     = 0x0f; // aka VARCHAR (?)         
        BIT,            //exports.BIT         = 0x10; // aka BIT, 1-8 byte
        NEWDECIMAL = 0xf6,//exports.NEWDECIMAL= 0xf6; // aka DECIMAL
        ENUM,           //exports.ENUM        = 0xf7; // aka ENUM
        SET,            //exports.SET         = 0xf8; // aka SET
        TINY_BLOB,      //exports.TINY_BLOB   = 0xf9; // aka TINYBLOB, TINYTEXT
        MEDIUM_BLOB,    //exports.MEDIUM_BLOB = 0xfa; // aka MEDIUMBLOB, MEDIUMTEXT
        LONG_BLOB,      //exports.LONG_BLOB   = 0xfb; // aka LONGBLOG, LONGTEXT
        BLOB,           //exports.BLOB        = 0xfc; // aka BLOB, TEXT
        VAR_STRING,     //exports.VAR_STRING  = 0xfd; // aka VARCHAR, VARBINARY
        STRING,         //exports.STRING      = 0xfe; // aka CHAR, BINARY
        GEOMETRY        //exports.GEOMETRY    = 0xff; // aka GEOMETRY
    }

    enum CursorFlags : byte
    {
        CURSOR_TYPE_NO_CURSOR = 0,
        CURSOR_TYPE_READ_ONLY = 1,
        CURSOR_TYPE_FOR_UPDATE = 2,
        CURSOR_TYPE_SCROLLABLE = 4
    }

    enum Command : byte
    {
        SLEEP = 0x00,   //SLEEP              : 0x00,  // deprecated
        QUIT,           //QUIT               : 0x01,
        INIT_DB,        //INIT_DB            : 0x02,
        QUERY,          //QUERY              : 0x03,
        FIELD_LIST,     //FIELD_LIST         : 0x04,
        CREATE_DB,      //CREATE_DB          : 0x05,
        DROP_DB,        //DROP_DB            : 0x06,
        REFRESH,        //REFRESH            : 0x07,
        SHUTDOWN,       //SHUTDOWN           : 0x08,
        STATISTICS,     //STATISTICS         : 0x09,
        PROCESS_INFO,   //PROCESS_INFO       : 0x0a,  // deprecated
        CONNECT,        //CONNECT            : 0x0b,  // deprecated
        PROCESS_KILL,   //PROCESS_KILL       : 0x0c,
        DEBUG,          //DEBUG              : 0x0d,
        PING,           //PING               : 0x0e,
        TIME,           //TIME               : 0x0f,  // deprecated
        DELAYED_INSERT, //DELAYED_INSERT     : 0x10,  // deprecated
        CHANGE_USER,    //CHANGE_USER        : 0x11,
        BINLOG_DUMP,    //BINLOG_DUMP        : 0x12,
        TABLE_DUMP,     //TABLE_DUMP         : 0x13,
        CONNECT_OUT,    //CONNECT_OUT        : 0x14,
        REGISTER_SLAVE, //REGISTER_SLAVE     : 0x15,
        STMT_PREPARE,   //STMT_PREPARE       : 0x16,
        STMT_EXECUTE,   //STMT_EXECUTE       : 0x17,
        STMT_SEND_LONG_DATA,//STMT_SEND_LONG_DATA: 0x18,
        STMT_CLOSE,     //STMT_CLOSE         : 0x19,
        STMT_RESET,     //STMT_RESET         : 0x1a,
        SET_OPTION,     //SET_OPTION         : 0x1b,
        STMT_FETCH,     //STMT_FETCH         : 0x1c,
        DAEMON,         //DAEMON             : 0x1d,  // deprecated
        BINLOG_DUMP_GTID,//BINLOG_DUMP_GTID  : 0x1e,
        UNKNOWN = 0xff    //UNKNOWN            : 0xff   // bad!
    }


    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    struct MyStructData
    {
        [System.Runtime.InteropServices.FieldOffset(0)]
        public int myInt32;
        [System.Runtime.InteropServices.FieldOffset(0)]
        public uint myUInt32;
        //---------------------------------------------
        [System.Runtime.InteropServices.FieldOffset(0)]
        public long myInt64;
        [System.Runtime.InteropServices.FieldOffset(0)]
        public ulong myUInt64;
        //---------------------------------------------

        [System.Runtime.InteropServices.FieldOffset(0)]
        public double myDouble;
        //---------------------------------------------
        [System.Runtime.InteropServices.FieldOffset(0)]
        public decimal myDecimal;//16-bytes
        [System.Runtime.InteropServices.FieldOffset(0)]
        public DateTime myDateTime;
        //---------------------------------------------
        [System.Runtime.InteropServices.FieldOffset(16)]
        public byte[] myBuffer;
        [System.Runtime.InteropServices.FieldOffset(16)]
        public string myString;
        [System.Runtime.InteropServices.FieldOffset(24)]
        public MySqlDataType type; //1  byte


        public override string ToString()
        {
            switch (type)
            {
                case MySqlDataType.TIMESTAMP:
                case MySqlDataType.DATE:
                case MySqlDataType.DATETIME:
                case MySqlDataType.NEWDATE:
                    return myDateTime.ToString();
                case MySqlDataType.TINY:
                case MySqlDataType.SHORT:
                case MySqlDataType.LONG:
                case MySqlDataType.INT24:
                case MySqlDataType.YEAR:
                    return myInt32.ToString();
                case MySqlDataType.FLOAT:
                case MySqlDataType.DOUBLE:
                    return myDouble.ToString();
                case MySqlDataType.NEWDECIMAL:
                    return myDecimal.ToString();
                case MySqlDataType.LONGLONG:
                    return myInt64.ToString();
                case MySqlDataType.BIT:
                    return myBuffer.ToString();
                case MySqlDataType.STRING:
                case MySqlDataType.VAR_STRING:
                    return myString;
                case MySqlDataType.TINY_BLOB:
                case MySqlDataType.MEDIUM_BLOB:
                case MySqlDataType.LONG_BLOB:
                case MySqlDataType.BLOB:
                    return myBuffer.ToString();
                case MySqlDataType.GEOMETRY:
                default: return base.ToString();
            }
        }
    }

    class Geometry
    {
        double _x;
        double _y;
        List<Geometry> geoValues;
        public Geometry()
        {
            geoValues = new List<Geometry>();
        }
        public Geometry(double x, double y)
        {
            _x = x;
            _y = y;
            geoValues = new List<Geometry>();
        }
        public void SetValue(double x, double y)
        {
            _x = x;
            _y = y;
        }
        public void AddChildValue(Geometry value)
        {
            geoValues.Add(value);
        }
        public void AddChildValues(List<Geometry> values)
        {
            geoValues = values;
        }
    }
}