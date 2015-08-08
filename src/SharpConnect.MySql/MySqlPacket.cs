//LICENSE: MIT
//Copyright(c) 2012 Felix Geisendörfer(felix @debuggable.com) and contributors 
//Copyright(c) 2015 brezza27, EngineKit and contributors

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
using System.Text;
using System.IO;

using System.Threading;
using System.Net;
using System.Net.Sockets;


namespace MySqlPacket
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

        UTF8_GENERAL50_CI = 253,  //exports.UTF8_GENERAL50_CI            = 253;

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

    enum Types
    {
        // Manually extracted from mysql-5.5.23/include/mysql_com.h
        // some more info here: http://dev.mysql.com/doc/refman/5.5/en/c-api-prepared-statement-type-codes.html
        DECIMAL = 0x00, //exports.DECIMAL     = 0x00; // aka DECIMAL (http://dev.mysql.com/doc/refman/5.0/en/precision-math-decimal-changes.html)
        TINY,           //exports.TINY        = 0x01; // aka TINYINT, 1 byte
        SHORT,          //exports.SHORT       = 0x02; // aka SMALLINT, 2 bytes
        LONG,           //exports.LONG        = 0x03; // aka INT, 4 bytes
        FLOAT,          //exports.FLOAT       = 0x04; // aka FLOAT, 4-8 bytes
        DOUBLE,         //exports.DOUBLE      = 0x05; // aka DOUBLE, 8 bytes
        NULL,           //exports.NULL        = 0x06; // NULL (used for prepared statements, I think)
        TIMESTAMP,      //exports.TIMESTAMP   = 0x07; // aka TIMESTAMP
        LONGLONG,       //exports.LONGLONG    = 0x08; // aka BIGINT, 8 bytes
        INT24,          //exports.INT24       = 0x09; // aka MEDIUMINT, 3 bytes
        DATE,           //exports.DATE        = 0x0a; // aka DATE
        TIME,           //exports.TIME        = 0x0b; // aka TIME
        DATETIME,       //exports.DATETIME    = 0x0c; // aka DATETIME
        YEAR,           //exports.YEAR        = 0x0d; // aka YEAR, 1 byte (don't ask)
        NEWDATE,        //exports.NEWDATE     = 0x0e; // aka ?
        VARCHAR,        //exports.VARCHAR     = 0x0f; // aka VARCHAR (?)
        BIT,            //exports.BIT         = 0x10; // aka BIT, 1-8 byte

        ERROR = 0x23,   //manual new create

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

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    struct MyStructData
    {
        [System.Runtime.InteropServices.FieldOffset(0)]
        public byte myByte;
        [System.Runtime.InteropServices.FieldOffset(0)]
        public short myInt16;
        [System.Runtime.InteropServices.FieldOffset(0)]
        public int myInt32;
        [System.Runtime.InteropServices.FieldOffset(0)]
        public long myLong;

        [System.Runtime.InteropServices.FieldOffset(0)]
        public float myFloat;
        [System.Runtime.InteropServices.FieldOffset(0)]
        public double myDouble;
        [System.Runtime.InteropServices.FieldOffset(0)]
        public decimal myDecimal;//16-bytes
        [System.Runtime.InteropServices.FieldOffset(0)]
        public DateTime myDateTime;

        [System.Runtime.InteropServices.FieldOffset(16)]
        public byte[] myBuffer;
        [System.Runtime.InteropServices.FieldOffset(16)]
        public string myString;

        [System.Runtime.InteropServices.FieldOffset(24)]
        public Types type;

        public override string ToString()
        {
            switch (type)
            {
                case Types.TIMESTAMP:
                case Types.DATE:
                case Types.DATETIME:
                case Types.NEWDATE:
                    return myDateTime.ToString();
                case Types.TINY:
                case Types.SHORT:
                case Types.LONG:
                case Types.INT24:
                case Types.YEAR:
                    return myInt32.ToString();
                case Types.FLOAT:
                case Types.DOUBLE:
                    return myDouble.ToString();
                case Types.NEWDECIMAL:
                    return myDecimal.ToString();
                case Types.LONGLONG:
                    return myLong.ToString();
                case Types.BIT:
                    return myBuffer.ToString();
                case Types.STRING:
                case Types.VAR_STRING:
                    return myString;
                case Types.TINY_BLOB:
                case Types.MEDIUM_BLOB:
                case Types.LONG_BLOB:
                case Types.BLOB:
                    return myBuffer.ToString();
                case Types.GEOMETRY:
                default: return base.ToString();
            }

        }
    }

    static class dbugConsole
    {
        [System.Diagnostics.Conditional("DEBUG")]
        public static void WriteLine(string str)
        {
            Console.WriteLine(str);
        }

    }

    class Connection
    {
        public ConnectionConfig config;
        public Socket socket;
        public Object protocol;
        public bool connectionCall;
        public string state;
        public uint threadId;



        HandshakePacket handshake;
        ClientAuthenticationPacket authPacket;
        Query query;

        PacketParser parser;
        PacketWriter writer;


        long MAX_ALLOWED_PACKET = 0;
        public Connection(ConnectionConfig userConfig)
        {
            this.config = userConfig;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            protocol = null;
            connectionCall = false;
            state = "disconnected";
            //this.config = options.config;

            //this._socket        = options.socket;
            //this._protocol      = new Protocol({config: this.config, connection: this});
            //this._connectCalled = false;
            //this.state          = "disconnected";
            //this.threadId       = null;
            switch ((CharSets)config.charsetNumber)
            {
                case CharSets.UTF8_GENERAL_CI:
                    parser = new PacketParser(Encoding.UTF8);
                    writer = new PacketWriter(Encoding.UTF8);
                    break;
                case CharSets.ASCII:
                    parser = new PacketParser(Encoding.ASCII);
                    writer = new PacketWriter(Encoding.ASCII);
                    break;
            }
        }

        public void Connect()
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(config.host), config.port);
            socket.Connect(endpoint);

            byte[] buffer = new byte[512];
            int count = socket.Receive(buffer);
            if (count < 512)
            {
                writer.Rewrite();
                parser.LoadNewBuffer(buffer, count);
                handshake = new HandshakePacket();
                handshake.ParsePacket(parser);
                this.threadId = handshake.threadId;
                byte[] token = MakeToken(config.password, GetScrollbleBuffer(handshake.scrambleBuff1, handshake.scrambleBuff2));
                writer.IncrementPacketNumber();

                //------------------------------------------
                authPacket = new ClientAuthenticationPacket();
                authPacket.SetValues(config.user, token, config.database, handshake.protocol41);
                authPacket.WritePacket(writer);

                byte[] sendBuff = writer.ToArray();
                byte[] receiveBuff = new byte[512];
                int sendNum = socket.Send(sendBuff);
                int receiveNum = socket.Receive(receiveBuff);

                parser.LoadNewBuffer(receiveBuff, receiveNum);
                if (receiveBuff[4] == 255)
                {
                    ErrPacket errPacket = new ErrPacket();
                    errPacket.ParsePacket(parser);
                    return;
                }
                else
                {
                    OkPacket okPacket = new OkPacket(handshake.protocol41);
                    okPacket.ParsePacket(parser);
                }
                writer.Rewrite();
                GetMaxAllowedPacket();
                if (MAX_ALLOWED_PACKET > 0)
                {
                    writer.SetMaxAllowedPacket(MAX_ALLOWED_PACKET);
                }
            }
        }

        void GetMaxAllowedPacket()
        {
            query = CreateQuery("SELECT @@global.max_allowed_packet", null);
            query.ExecuteQuery();
            if (query.loadError != null)
            {
                dbugConsole.WriteLine("Error Message : " + query.loadError.message);
            }
            else if (query.okPacket != null)
            {

                dbugConsole.WriteLine("OkPacket : " + query.okPacket.affectedRows);

            }
            else
            {
                int i = 0;
                if (query.ReadRow())
                {
                    MAX_ALLOWED_PACKET = query.GetFieldData(0).myLong;
                    //MAX_ALLOWED_PACKET = query.resultSet.rows[0].GetDataInField("@@global.max_allowed_packet").myLong;
                    //dbugConsole.WriteLine("Rows Data " + i + " : " + query.resultSet.rows[i++]);
                }

                //while (query.ReadRow())
                //{

                //    MAX_ALLOWED_PACKET = query.resultSet.rows[0].GetDataInField("@@global.max_allowed_packet").myLong;
                //    dbugConsole.WriteLine("Rows Data " + i + " : " + query.resultSet.rows[i++]);
                //}
            }
        }

        public Query CreateQuery(string sql, PrepareStatement values)
        {
            //var query = Connection.createQuery(sql, values, cb);
            //query = new Query(parser, writer, sql, values);
            //query.typeCast = config.typeCast;
            //query.Start(socket, handshake.protocol41, config);
            if (socket == null)
            {
                CreateNewSocket();
            }

            query = new Query(parser, writer, sql, values, socket, handshake.protocol41, config, threadId);
            if (MAX_ALLOWED_PACKET > 0)
            {
                query.SetMaxSend(MAX_ALLOWED_PACKET);
            }
            return query;
        }

        void CreateNewSocket()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Connect();
        }

        public void Disconnect()
        {
            writer.Rewrite();
            ComQuitPacket quitPacket = new ComQuitPacket();
            quitPacket.WritePacket(writer);

            int send = socket.Send(writer.ToArray());
            socket.Disconnect(true);
        }
        public bool IsStoredInConnPool { get; set; }
        public bool IsInUsed { get; set; }

        static byte[] GetScrollbleBuffer(byte[] part1, byte[] part2)
        {
            return ConcatBuffer(part1, part2);
        }

        static byte[] MakeToken(string password, byte[] scramble)
        {
            // password must be in binary format, not utf8
            //var stage1 = sha1((new Buffer(password, "utf8")).toString("binary"));
            //var stage2 = sha1(stage1);
            //var stage3 = sha1(scramble.toString('binary') + stage2);
            //return xor(stage3, stage1);
            var buff1 = Encoding.UTF8.GetBytes(password.ToCharArray());

            var sha = new System.Security.Cryptography.SHA1Managed();
            // This is one implementation of the abstract class SHA1.
            //scramble = new byte[] { 52, 78, 110, 96, 117, 75, 85, 75, 87, 83, 121, 44, 106, 82, 62, 123, 113, 73, 84, 77 };
            byte[] stage1 = sha.ComputeHash(buff1);
            byte[] stage2 = sha.ComputeHash(stage1);
            //merge scramble and stage2 again
            byte[] combineFor3 = ConcatBuffer(scramble, stage2);
            byte[] stage3 = sha.ComputeHash(combineFor3);

            var final = xor(stage3, stage1);
            return final;
        }

        static byte[] ConcatBuffer(byte[] a, byte[] b)
        {
            byte[] combine = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, combine, 0, a.Length);
            Buffer.BlockCopy(b, 0, combine, a.Length, b.Length);
            return combine;
        }

        static byte[] xor(byte[] a, byte[] b)
        {
            var result = new byte[a.Length];
            int j = a.Length;
            for (int i = 0; i < j; ++i)
            {
                result[i] = (byte)(a[i] ^ b[i]);
            }
            return result;
        }
    }


    class ConnectionConfig
    {
        public string host;
        public int port;
        public string localAddress;//unknowed type
        public string socketPath;//unknowed type
        public string user;
        public string password;
        public string database;
        public int connectionTimeout;
        public bool insecureAuth;
        public bool supportBigNumbers;
        public bool bigNumberStrings;
        public bool dateStrings;
        public bool debug;
        public bool trace;
        public bool stringifyObjects;
        public string timezone;
        public string flags;
        public string queryFormat;
        public string pool;//unknowed type
        public string ssl;//string or bool
        public bool multipleStatements;
        public bool typeCast;
        public long maxPacketSize;
        public int charsetNumber;
        public int defaultFlags;
        public int clientFlags;

        public ConnectionConfig()
        {
            SetDefault();
        }

        public ConnectionConfig(string username, string password)
        {
            SetDefault();
            this.user = username;
            this.password = password;
        }
        public ConnectionConfig(string host, string username, string password, string database)
        {
            SetDefault();
            this.user = username;
            this.password = password;
            this.host = host;
            this.database = database;
        }
        void SetDefault()
        {
            //if (typeof options === 'string') {
            //  options = ConnectionConfig.parseUrl(options);
            //}
            host = "127.0.0.1";//this.host = options.host || 'localhost';
            port = 3306;//this.port = options.port || 3306;
            //this.localAddress       = options.localAddress;
            //this.socketPath         = options.socketPath;
            //this.user               = options.user || undefined;
            //this.password           = options.password || undefined;
            //this.database           = options.database;
            database = "";
            connectionTimeout = 10 * 1000;
            //this.connectTimeout     = (options.connectTimeout === undefined)
            //  ? (10 * 1000)
            //  : options.connectTimeout;
            insecureAuth = false;//this.insecureAuth = options.insecureAuth || false;
            supportBigNumbers = false;//this.supportBigNumbers = options.supportBigNumbers || false;
            bigNumberStrings = false;//this.bigNumberStrings = options.bigNumberStrings || false;
            dateStrings = false;//this.dateStrings = options.dateStrings || false;
            debug = false;//this.debug = options.debug || true;
            trace = false;//this.trace = options.trace !== false;
            stringifyObjects = false;//this.stringifyObjects = options.stringifyObjects || false;
            timezone = "local";//this.timezone = options.timezone || 'local';
            flags = "";//this.flags = options.flags || '';
            //this.queryFormat        = options.queryFormat;
            //this.pool               = options.pool || undefined;

            //this.ssl                = (typeof options.ssl === 'string')
            //  ? ConnectionConfig.getSSLProfile(options.ssl)
            //  : (options.ssl || false);
            multipleStatements = false;//this.multipleStatements = options.multipleStatements || false; 
            typeCast = true;
            //this.typeCast = (options.typeCast === undefined)
            //  ? true
            //  : options.typeCast;

            //if (this.timezone[0] == " ") {
            //  // "+" is a url encoded char for space so it
            //  // gets translated to space when giving a
            //  // connection string..
            //  this.timezone = "+" + this.timezone.substr(1);
            //}

            //if (this.ssl) {
            //  // Default rejectUnauthorized to true
            //  this.ssl.rejectUnauthorized = this.ssl.rejectUnauthorized !== false;
            //}

            maxPacketSize = 0;//this.maxPacketSize = 0;
            charsetNumber = (int)CharSets.UTF8_GENERAL_CI;
            //this.charsetNumber = (options.charset)
            //  ? ConnectionConfig.getCharsetNumber(options.charset)
            //  : options.charsetNumber||Charsets.UTF8_GENERAL_CI;

            //// Set the client flags
            //var defaultFlags = ConnectionConfig.getDefaultFlags(options);
            //this.clientFlags = ConnectionConfig.mergeFlags(defaultFlags, options.flags)
        }

        public void SetConfig(string host, int port, string username, string password, string database)
        {
            this.host = host;
            this.port = port;
            this.user = username;
            this.password = password;
            this.database = database;
        }
    }

    class MySqlProtocol
    {
        //Stream.call(this);

        //options = options || {};

        //this.readable = true;
        //this.writable = true;
        bool readable;
        bool writable;
        ConnectionConfig config;
        Connection connection;
        //this._config                        = options.config || {};
        //this._connection                    = options.connection;
        //this._callback                      = null;
        //this._fatalError                    = null;
        //this._quitSequence                  = null;
        //this._handshakeSequence             = null;
        //this._handshaked                    = false;
        //this._ended                         = false;
        //this._destroyed                     = false;
        //this._queue                         = [];
        //this._handshakeInitializationPacket = null;

        //this._parser = new Parser({
        //  onError  : this.handleParserError.bind(this),
        //  onPacket : this._parsePacket.bind(this),
        //  config   : this._config
        //});

    }

    class Query
    {
        public string sql;
        public PrepareStatement values;
        public bool typeCast;
        public bool nestTables;
        //public ResultSet resultSet;
        TableHeader tableHeader;
        public ErrPacket loadError;
        public OkPacket okPacket;
        public int index;

        RowDataPacket lastRow;
        //public string results;//unknowed value type
        //public string fields;//unknowed value type
        PacketParser parser;
        PacketWriter writer;

        Socket socket;
        bool protocol41;
        ConnectionConfig config;
        uint threadId;

        byte[] receiveBuffer;
        const int DEFAULT_BUFFER_SIZE = 512;
        const byte ERROR_CODE = 255;
        const byte EOF_CODE = 0xfe;
        const byte OK_CODE = 0;

        long MAX_ALLOWED_SEND = 0;

        public Query(PacketParser parser, PacketWriter writer, string sql, PrepareStatement values)
        {
            //Sequence.call(this, options, callback);
            //this.sql = options.sql;
            //this.values = options.values;
            //this.typeCast = (options.typeCast === undefined)
            //  ? true
            //  : options.typeCast;
            //this.nestTables = options.nestTables || false;

            //this._resultSet = null;
            //this._results   = [];
            //this._fields    = [];
            //this._index     = 0;
            //this._loadError = null;

            this.sql = sql;
            this.values = values;
            typeCast = true;
            nestTables = false;
            //resultSet = null;
            //this._results   = [];
            //this._fields    = [];
            index = 0;
            loadError = null;

            this.parser = parser;
            this.writer = writer;

            this.sql = SqlFormat(sql, values);
        }

        public Query(PacketParser parser, PacketWriter writer, string sql, PrepareStatement values, Socket socket, bool protocol41, ConnectionConfig config, uint threadId)
        {
            this.sql = sql;
            this.values = values;
            typeCast = config.typeCast;
            nestTables = false;
            //resultSet = null;
            //this._results   = [];
            //this._fields    = [];
            index = 0;
            loadError = null;

            this.parser = parser;
            this.writer = writer;

            this.sql = SqlFormat(sql, values);

            this.socket = socket;
            this.protocol41 = protocol41;
            this.config = config;
            this.receiveBuffer = null;

            this.threadId = threadId;
        }

        public void SetMaxSend(long max)
        {
            MAX_ALLOWED_SEND = max;
        }

        //public void Start(Socket socket, bool protocol41, ConnectionConfig config)
        //{
        //    this.socket = socket;
        //    this.config = config;
        //    this.protocol41 = protocol41;
        //    writer.Rewrite();
        //    ComQueryPacket queryPacket = new ComQueryPacket(sql);
        //    queryPacket.WritePacket(writer);

        //    byte[] qr = writer.ToArray();
        //    int sent = socket.Send(qr);

        //    byte[] receiveBuffer = new byte[DEFAULT_BUFFER_SIZE];
        //    int receive = socket.Receive(receiveBuffer);

        //    parser.LoadNewBuffer(receiveBuffer, receive);
        //    if (receiveBuffer[4] == ERROR_CODE)
        //    {
        //        loadError = new ErrPacket();
        //        loadError.ParsePacket(parser);
        //    }
        //    else if (receiveBuffer[4] == OK_CODE)
        //    {
        //        okPacket = new OkPacket(protocol41);
        //        okPacket.ParsePacket(parser);
        //    }
        //    else
        //    {
        //        ResultSetHeaderPacket resultPacket = new ResultSetHeaderPacket();
        //        resultPacket.ParsePacket(parser);
        //        resultSet = new ResultSet(resultPacket);

        //        while (receiveBuffer[parser.Position + 4] != EOF_CODE)
        //        {
        //            FieldPacket fieldPacket = new FieldPacket(protocol41);
        //            fieldPacket.ParsePacketHeader(parser);
        //            receiveBuffer = CheckLimit(fieldPacket.GetPacketLength(), receiveBuffer, (int)parser.Length);
        //            fieldPacket.ParsePacket(parser);
        //            resultSet.Add(fieldPacket);

        //            receiveBuffer = CheckBeforeParseHeader(receiveBuffer, (int)parser.Position, (int)parser.Length);
        //        }

        //        EofPacket fieldEof = new EofPacket(protocol41);//if temp[4]=0xfe then eof packet
        //        fieldEof.ParsePacketHeader(parser);
        //        receiveBuffer = CheckLimit(fieldEof.GetPacketLength(), receiveBuffer, (int)parser.Length);
        //        fieldEof.ParsePacket(parser);
        //        resultSet.Add(fieldEof);

        //        receiveBuffer = CheckBeforeParseHeader(receiveBuffer, (int)parser.Position, (int)parser.Length);

        //        var fieldList = resultSet.GetFields();
        //        while (receiveBuffer[parser.Position + 4] != EOF_CODE)
        //        {
        //            RowDataPacket rowData = new RowDataPacket(fieldList, typeCast, nestTables, config);
        //            rowData.ParsePacketHeader(parser);
        //            receiveBuffer = CheckLimit(rowData.GetPacketLength(), receiveBuffer, (int)parser.Length);
        //            rowData.ParsePacket(parser);
        //            resultSet.Add(rowData);

        //            receiveBuffer = CheckBeforeParseHeader(receiveBuffer, (int)parser.Position, (int)parser.Length);
        //        }

        //        EofPacket rowDataEof = new EofPacket(protocol41);
        //        rowDataEof.ParsePacketHeader(parser);
        //        receiveBuffer = CheckLimit(rowDataEof.GetPacketLength(), receiveBuffer, (int)parser.Length);
        //        rowDataEof.ParsePacket(parser);
        //        resultSet.Add(rowDataEof);
        //    }
        //}

        public void ExecuteQuery()
        {
            SendQuery(sql);

            receiveBuffer = new byte[DEFAULT_BUFFER_SIZE];

            int receive = socket.Receive(receiveBuffer);

            parser.LoadNewBuffer(receiveBuffer, receive);
            if (receive == 0)
            {
                return;
            }
            switch (receiveBuffer[4])
            {
                case ERROR_CODE:
                    loadError = new ErrPacket();
                    loadError.ParsePacket(parser);
                    break;
                case OK_CODE:
                    okPacket = new OkPacket(protocol41);
                    okPacket.ParsePacket(parser);
                    break;
                default:
                    ParseResultSet();
                    break;
            }
        }

        void SendQuery(string sql)
        {
            writer.Rewrite();
            ComQueryPacket queryPacket = new ComQueryPacket(sql);
            queryPacket.WritePacket(writer);
            byte[] qr = writer.ToArray();
            int sent = 0;
            //if send data more than max_allowed_packet in mysql server it will be close connection
            if (MAX_ALLOWED_SEND > 0 && qr.Length > MAX_ALLOWED_SEND)
            {
                int packs = (int)Math.Floor(qr.Length / (double)MAX_ALLOWED_SEND) + 1;
                for (int pack = 0; pack < packs; pack++)
                {
                    sent = socket.Send(qr, (int)MAX_ALLOWED_SEND, SocketFlags.None);
                }
            }
            else
            {
                sent = socket.Send(qr, qr.Length, SocketFlags.None);
            }
        }

        void ParseResultSet()
        {
            ResultSetHeaderPacket resultPacket = new ResultSetHeaderPacket();
            resultPacket.ParsePacket(parser);
            //resultSet = new ResultSet(resultPacket);

            this.tableHeader = new TableHeader();
            tableHeader.TypeCast = typeCast;
            tableHeader.NestTables = nestTables;
            tableHeader.ConnConfig = config;

            while (receiveBuffer[parser.Position + 4] != EOF_CODE)
            {
                FieldPacket fieldPacket = new FieldPacket(protocol41);
                fieldPacket.ParsePacketHeader(parser);
                receiveBuffer = CheckLimit(fieldPacket.GetPacketLength(), receiveBuffer, DEFAULT_BUFFER_SIZE);
                fieldPacket.ParsePacket(parser);
                tableHeader.AddField(fieldPacket);
                receiveBuffer = CheckBeforeParseHeader(receiveBuffer, (int)parser.Position, DEFAULT_BUFFER_SIZE);
            }



            EofPacket fieldEof = new EofPacket(protocol41);//if temp[4]=0xfe then eof packet
            fieldEof.ParsePacketHeader(parser);
            receiveBuffer = CheckLimit(fieldEof.GetPacketLength(), receiveBuffer, DEFAULT_BUFFER_SIZE);
            fieldEof.ParsePacket(parser);
            //resultSet.Add(fieldEof);

            receiveBuffer = CheckBeforeParseHeader(receiveBuffer, (int)parser.Position, DEFAULT_BUFFER_SIZE);

            //-----
            lastRow = new RowDataPacket(tableHeader);

        }

        public bool ReadRow()
        {
            if (tableHeader == null)
            {
                return false;
            }


            switch (receiveBuffer[parser.Position + 4])
            {
                case ERROR_CODE:
                    {
                        loadError = new ErrPacket();
                        loadError.ParsePacket(parser);
                        return false;
                    }
                case EOF_CODE:
                    {
                        EofPacket rowDataEof = new EofPacket(protocol41);
                        rowDataEof.ParsePacketHeader(parser);
                        receiveBuffer = CheckLimit(rowDataEof.GetPacketLength(), receiveBuffer, DEFAULT_BUFFER_SIZE);
                        rowDataEof.ParsePacket(parser);

                        //resultSet.Add(rowDataEof);
                        return false;
                    }
                default:
                    {
                        dbugConsole.WriteLine("Before parse [Position] : " + parser.Position);

                        lastRow.ReuseSlots();
                        lastRow.ParsePacketHeader(parser);

                        dbugConsole.WriteLine("After parse header [Position] : " + parser.Position);

                        receiveBuffer = CheckLimit(lastRow.GetPacketLength(), receiveBuffer, DEFAULT_BUFFER_SIZE);
                        lastRow.ParsePacket(parser);
                        //resultSet.Add(rowData);
                        receiveBuffer = CheckBeforeParseHeader(receiveBuffer, (int)parser.Position, DEFAULT_BUFFER_SIZE);
                        dbugConsole.WriteLine("After parse Row [Position] : " + parser.Position);


                        return true;
                    }
            }
        }

        public MyStructData GetFieldData(string fieldName)
        {
            return lastRow.GetDataInField(fieldName);
        }
        public MyStructData GetFieldData(int fieldIndex)
        {
            return lastRow.GetDataInField(fieldIndex);
        }

        public void Close()
        {
            //sql = "RESET QUERY CACHE";//not work
            //writer.Rewrite();
            //ComQuitPacket quitPacket = new ComQuitPacket();
            //quitPacket.WritePacket(writer);
            //int send = socket.Send(writer.ToArray());
            //sql = "KILL " + threadId;
            //Connection connection = new Connection(config);
            //connection.Connect();
            //Query q = connection.CreateQuery(sql, null);
            //sql = "RESET QUERY CACHE";
            //sql = SqlFormat(sql, null);
            //SendQuery(sql);
            //Console.WriteLine("sql : '" + sql + "'");
            //sql = "KILL " + threadId;
            //sql = SqlFormat(sql, null);
            //SendQuery(sql);
            //Console.WriteLine("sql : '" + sql + "'");
            //Thread.Sleep(1000);
            //socket.Disconnect(false);
            //this.Disconnect();
            int lastReceive = 0;
            long allReceive = 0;
            byte[] temp = new byte[65536];
            int i = 0;
            while (socket.Available > 0)
            {
                lastReceive = socket.Receive(temp);
                allReceive += lastReceive;
                i++;
                Console.WriteLine("i : " + i + ", lastReceive : " + lastReceive);
                Thread.Sleep(100);
            }

            dbugConsole.WriteLine("All Receive bytes : " + allReceive);
            //socket = null;
            ////TODO :test
            //int test = 44;
            //if (lastReceive > test)
            //{
            //    byte[] dd = new byte[test];
            //    dd = CopyBufferBlock(temp, lastReceive - test, test);

            //    parser.LoadNewBuffer(dd, test);
            //    loadError = new ErrPacket();
            //    loadError.ParsePacket(parser);
            //}
            //socket.Disconnect(false);

            //Connection newConnect = new Connection(config);
            //newConnect.Connect();
            //socket = newConnect.socket;
        }

        void Disconnect()
        {
            writer.Rewrite();
            ComQuitPacket quitPacket = new ComQuitPacket();
            quitPacket.WritePacket(writer);

            int send = socket.Send(writer.ToArray());
            //socket.Disconnect(true);
        }

        byte[] CheckLimit(uint packetLength, byte[] buffer, int limit)
        {
            int remainLength = (int)(parser.Length - parser.Position);
            if (packetLength > remainLength)
            {
                byte[] remainBuff = CopyBufferBlock(buffer, (int)parser.Position, remainLength);
                byte[] receiveBuff;
                int packetRemainLength = (int)packetLength - remainLength;
                if (packetRemainLength > limit)
                {
                    receiveBuff = new byte[packetRemainLength];
                }
                else
                {
                    receiveBuff = new byte[limit];
                }
                int newBufferLength = receiveBuff.Length + remainLength;
                if (newBufferLength > buffer.Length)
                {
                    buffer = new byte[newBufferLength];
                }
                remainBuff.CopyTo(buffer, 0);

                int newReceive = socket.Receive(receiveBuff);
                if (newReceive < newBufferLength)//รับมาได้ไม่หมด
                {
                    int newIndex = 0;
                    byte[] temp = new byte[newReceive];
                    temp = CopyBufferBlock(receiveBuff, 0, newReceive);
                    temp.CopyTo(buffer, remainLength);
                    newIndex = newReceive + remainLength;
                    while (newIndex < newBufferLength)
                    {
                        Thread.Sleep(100);
                        var s = socket.Available;
                        if (s == 0)
                        {
                            break;
                        }
                        newReceive = socket.Receive(receiveBuff);
                        if (newReceive > 0)
                        {
                            if (newReceive + newIndex + remainLength > newBufferLength)
                            {
                                temp = new byte[newReceive];
                                temp = CopyBufferBlock(receiveBuff, 0, newReceive);
                                byte[] bytes;// = new byte[newReceive + newIndex];
                                bytes = CopyBufferBlock(buffer, 0, newIndex);
                                buffer = new byte[newReceive + newIndex];
                                bytes.CopyTo(buffer, 0);
                                temp.CopyTo(buffer, newIndex);
                                newIndex += newReceive;
                            }
                            else
                            {
                                temp = new byte[newReceive];
                                temp = CopyBufferBlock(receiveBuff, 0, newReceive);
                                temp.CopyTo(buffer, remainLength + newIndex);
                                newIndex += newReceive;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    newBufferLength = newIndex;
                }
                else
                {
                    receiveBuff.CopyTo(buffer, remainLength);
                }
                parser.LoadNewBuffer(buffer, newBufferLength);
            }
            return buffer;
        }

        byte[] CopyBufferBlock(byte[] inputBuffer, int start, int length)
        {
            byte[] outputBuff = new byte[length];
            for (int index = 0; index < length; index++)
            {
                outputBuff[index] = inputBuffer[start + index];
            }
            return outputBuff;
        }

        byte[] CheckBeforeParseHeader(byte[] buffer, int position, int limit)
        {
            int remainLength = (int)parser.Length - position;
            if (remainLength < 5)//5 bytes --> 4 bytes from header and 1 byte for find packet type
            {
                byte[] remainBuff = CopyBufferBlock(buffer, position, remainLength);
                byte[] receiveBuff = new byte[limit];
                int newReceive = socket.Receive(receiveBuff);
                int newBufferLength = newReceive + remainLength;
                if (newBufferLength > buffer.Length)
                {
                    buffer = new byte[newBufferLength];
                }
                remainBuff.CopyTo(buffer, 0);
                receiveBuff.CopyTo(buffer, remainLength);
                //buffer = remainBuff.Concat(receiveBuff).ToArray();
                parser.LoadNewBuffer(buffer, newReceive + remainLength);

                dbugConsole.WriteLine("CheckBeforeParseHeader : LoadNewBuffer");
            }
            return buffer;
        }

        string SqlFormat(string sql, PrepareStatement values)
        {
            if (values == null)
            {
                return sql;
            }
            return ParseSqlFormat(sql, values);
        }

        enum ParseState
        {
            FIND_MARKER,
            GET_KEY
        }

        string ParseSqlFormat(string sql, PrepareStatement prepare)
        {
            int length = sql.Length;
            ParseState state = ParseState.FIND_MARKER;
            char ch;
            StringBuilder strBuilder = new StringBuilder();
            List<string> list = new List<string>();
            string temp;
            for (int i = 0; i < length; i++)
            {
                ch = sql[i];
                switch (state)
                {
                    case ParseState.FIND_MARKER:
                        if (ch == '?')
                        {
                            list.Add(strBuilder.ToString());
                            strBuilder.Clear();

                            state = ParseState.GET_KEY;
                        }
                        else
                        {
                            strBuilder.Append(ch);
                        }
                        break;
                    case ParseState.GET_KEY:
                        if (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9')
                        {
                            strBuilder.Append(ch);
                        }
                        else
                        {
                            temp = prepare.GetValue(strBuilder.ToString());
                            list.Add(temp);
                            strBuilder.Clear();
                            state = ParseState.FIND_MARKER;
                            strBuilder.Append(ch);
                        }
                        break;
                    default:
                        break;
                }
            }
            temp = strBuilder.ToString();
            if (state == ParseState.GET_KEY)
            {
                temp = prepare.GetValue(temp);
            }
            list.Add(temp);
            return GetSql(list);
        }

        string GetSql(List<string> list)
        {
            int length = list.Count;
            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                strBuilder.Append(list[i]);
            }
            return strBuilder.ToString();
        }
    }

    class PrepareStatement
    {
        Dictionary<string, string> prepareValues;

        public PrepareStatement()
        {
            prepareValues = new Dictionary<string, string>();
        }

        void AddOrChangeValue(string key, string value)
        {
            string temp;
            if (prepareValues.TryGetValue(key, out temp))
            {
                prepareValues[key] = value;
            }
            else
            {
                prepareValues.Add(key, value);
            }
        }

        public void AddTable(string key, string value)
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.Append("`");
            strBuilder.Append(value);
            strBuilder.Append("`");
            //prepareValues.Add(key, strBuilder.ToString());
            AddOrChangeValue(key, strBuilder.ToString());
        }

        public void AddField(string key, string value)
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.Append("`");
            strBuilder.Append(value);
            strBuilder.Append("`");
            //prepareValues.Add(key, strBuilder.ToString());
            AddOrChangeValue(key, strBuilder.ToString());
        }

        public void AddValue(string key, string value)
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.Append("'");
            strBuilder.Append(value);
            strBuilder.Append("'");
            //prepareValues.Add(key, strBuilder.ToString());
            AddOrChangeValue(key, strBuilder.ToString());
        }

        public void AddValue(string key, decimal value)
        {
            //prepareValues.Add(key, value.ToString());
            AddOrChangeValue(key, value.ToString());
        }

        public void AddValue(string key, int value)
        {
            //prepareValues.Add(key, value.ToString());
            AddOrChangeValue(key, value.ToString());
        }

        public void AddValue(string key, long value)
        {
            //prepareValues.Add(key, value.ToString());
            AddOrChangeValue(key, value.ToString());
        }

        public void AddValue(string key, byte value)
        {
            string str = Encoding.ASCII.GetString(new byte[] { value });
            //prepareValues.Add(key, str);
            AddOrChangeValue(key, str);
        }

        public void AddValue(string key, byte[] value)
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.Append("0x");
            strBuilder.Append(ByteArrayToString(value));
            string str = strBuilder.ToString();
            //prepareValues.Add(key, str);
            AddOrChangeValue(key, str);
        }

        public string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public void AddValue(string key, DateTime value)
        {
            //prepareValues.Add(key, value.ToString());
            AddOrChangeValue(key, value.ToString());
        }

        public string GetValue(string key)
        {
            string value = "";
            if (prepareValues.TryGetValue(key, out value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }
    }


    class TableHeader
    {
        List<FieldPacket> fields;
        Dictionary<string, int> fieldNamePosMap;
        bool typeCast;
        bool nestTables;

        public TableHeader()
        {
            this.fields = new List<FieldPacket>();
        }

        public void AddField(FieldPacket field)
        {
            fields.Add(field);
        }
        public List<FieldPacket> GetFields()
        {
            return fields;
        }
        public int ColumnCount
        {
            get { return this.fields.Count; }
        }
        public int GetFieldIndex(string fieldName)
        {
            if (fieldNamePosMap == null)
            {
                ///build map index
                int j = fields.Count;
                fieldNamePosMap = new Dictionary<string, int>(j);
                for (int i = 0; i < j; ++i)
                {
                    fieldNamePosMap.Add(fields[i].name, i);
                }
            }


            int found;
            if (!fieldNamePosMap.TryGetValue(fieldName, out found))
            {
                return -1;
            }
            return found;
        }

        public bool TypeCast { get; set; }
        public bool NestTables { get; set; }
        public ConnectionConfig ConnConfig { get; set; }
    }

    //class ResultSet
    //{
    //    TableHeader tableHeader;
    //    ResultSetHeaderPacket resultSetHeaderPacket;
    //    List<FieldPacket> fieldPackets;
    //    public List<RowDataPacket> rows;
    //    public ResultSet(ResultSetHeaderPacket resultHeader)
    //    {
    //        resultSetHeaderPacket = resultHeader;
    //        fieldPackets = new List<FieldPacket>();
    //        rows = new List<RowDataPacket>();
    //    }
    //    public void Add(FieldPacket packet)
    //    {
    //        fieldPackets.Add(packet);
    //    }
    //    public void Add(RowDataPacket packet)
    //    {
    //        rows.Add(packet);
    //    }
    //    public void Add(EofPacket packet)
    //    {

    //    }


    //    public TableHeader GetTableHeader()
    //    {
    //        if (tableHeader == null)
    //        {
    //            return tableHeader = new TableHeader(fieldPackets);
    //        }
    //        else
    //        {
    //            return tableHeader;
    //        }
    //    }

    //}

    //class FieldInfo
    //{

    //}



    class PacketParser
    {
        BinaryReader reader;
        MemoryStream stream;
        int myLength;
        public long Position
        {
            get { return stream.Position; }

        }
        public long Length
        {
            get
            {
                return myLength;
            }
        }
        long startPosition;
        long packetLength;
        Encoding encoding = Encoding.UTF8;

        public PacketParser(Encoding encoding)
        {
            this.encoding = encoding;
            stream = new MemoryStream();
            startPosition = stream.Position;//stream.Position = 0;
            reader = new BinaryReader(stream, encoding);
        }

        ~PacketParser()
        {
            Dispose();
        }

        public void Dispose()
        {
            reader.Close();
            stream.Close();
            stream.Dispose();
        }

        public void Reparse()
        {
            stream.Position = 0;
            myLength = 0;
        }

        public void LoadNewBuffer(byte[] newBuffer, int count)
        {
            Reparse();
            stream.Write(newBuffer, 0, count);
            stream.Position = 0;
            startPosition = 0;
            myLength = count;
        }

        public string ParseNullTerminatedString()
        {
            List<byte> bList = new List<byte>();
            byte temp = reader.ReadByte();
            bList.Add(temp);
            while (temp != 0)
            {
                temp = reader.ReadByte();
                bList.Add(temp);
            }
            byte[] bytes = bList.ToArray();
            return encoding.GetString(bytes);
        }

        public byte[] ParseNullTerminatedBuffer()
        {
            List<byte> list = new List<byte>();
            var temp = reader.ReadByte();
            list.Add(temp);
            while (temp != 0x00)
            {
                temp = reader.ReadByte();
                list.Add(temp);
            }
            return list.ToArray();
        }

        public byte ParseByte()
        {
            return reader.ReadByte();
        }

        public byte[] ParseBuffer(int n)
        {
            if (n > 0)
                return reader.ReadBytes(n);
            else
                return null;
        }

        public PacketHeader ParsePacketHeader()
        {
            startPosition = stream.Position;
            PacketHeader header = new PacketHeader(ParseUnsignedNumber(3), ParseByte());
            packetLength = header.Length + 4;
            return header;
        }

        public string ParseLengthCodedString()
        {
            //var length = this.parseLengthCodedNumber();
            uint length = ParseLengthCodedNumber();
            //if (length === null) {
            //  return null;
            //}
            return ParseString(length);
            //return this.parseString(length);
        }

        public byte[] ParseLengthCodedBuffer()
        {
            //var length = this.parseLengthCodedNumber();
            uint length = ParseLengthCodedNumber();
            //  if (length === null) {
            //    return null;
            //  }
            return ParseBuffer((int)length);
            //  return this.parseBuffer(length);
        }

        public byte[] ParseFiller(int length)
        {
            return ParseBuffer(length);
        }

        public uint ParseLengthCodedNumber()
        {
            //if (this._offset >= this._buffer.length)
            //    {
            //        var err = new Error('Parser: read past end');
            //        err.offset = (this._offset - this._packetOffset);
            //        err.code = 'PARSER_READ_PAST_END';
            //        throw err;
            //    }
            if (Position >= Length)
            {
                throw new Exception("Parser: read past end");
            }
            //    var bits = this._buffer[this._offset++];

            byte bits = reader.ReadByte();

            //    if (bits <= 250)
            //    {
            //        return bits;
            //    }

            if (bits <= 250)
            {
                return bits;
            }
            //    switch (bits)
            //    {
            //        case 251:
            //            return null;
            //        case 252:
            //            return this.parseUnsignedNumber(2);
            //        case 253:
            //            return this.parseUnsignedNumber(3);
            //        case 254:
            //            break;
            //        default:
            //            var err = new Error('Unexpected first byte' + (bits ? ': 0x' + bits.toString(16) : ''));
            //            err.offset = (this._offset - this._packetOffset - 1);
            //            err.code = 'PARSER_BAD_LENGTH_BYTE';
            //            throw err;
            //    }

            switch (bits)
            {
                case 251: return 0;
                case 252: return this.ParseUnsignedNumber(2);
                case 253: return this.ParseUnsignedNumber(3);
                case 254: break;
                default: throw new Exception("Unexpected first byte");
            }
            //    var low = this.parseUnsignedNumber(4);
            //    var high = this.parseUnsignedNumber(4);
            //    var value;
            uint low = this.ParseUnsignedNumber(4);
            uint high = this.ParseUnsignedNumber(4);
            return 0;
            //    if (high >>> 21)
            //    {
            //        value = (new BigNumber(low)).plus((new BigNumber(MUL_32BIT)).times(high)).toString();

            //        if (this._supportBigNumbers)
            //        {
            //            return value;
            //        }

            //        var err = new Error(
            //          'parseLengthCodedNumber: JS precision range exceeded, ' +
            //          'number is >= 53 bit: "' + value + '"'
            //        );
            //        err.offset = (this._offset - this._packetOffset - 8);
            //        err.code = 'PARSER_JS_PRECISION_RANGE_EXCEEDED';
            //        throw err;
            //    }

            //    value = low + (MUL_32BIT * high);

            //    return value;
        }

        public uint ParseUnsignedNumber(int n)
        {
            //if (bytes === 1)
            //{
            //    return this._buffer[this._offset++];
            //}
            if (n == 1)
            {
                return reader.ReadByte();
            }
            //var buffer = this._buffer;
            //var offset = this._offset + bytes - 1;
            //var value = 0;

            //if (bytes > 4)
            //{
            //    var err = new Error('parseUnsignedNumber: Supports only up to 4 bytes');
            //    err.offset = (this._offset - this._packetOffset - 1);
            //    err.code = 'PARSER_UNSIGNED_TOO_LONG';
            //    throw err;
            //}
            if (n > 4)
            {
                throw new Exception("parseUnsignedNumber: Supports only up to 4 bytes");
            }

            long start = Position;
            long end = start + n - 1;

            //while (offset >= this._offset)
            //{
            //    value = ((value << 8) | buffer[offset]) >>> 0;
            //    offset--;
            //}
            byte[] temp = new byte[n];
            uint value = 0;
            for (int i = n - 1; i >= 0; i--)
            {
                temp[i] = reader.ReadByte();
                value = temp[i];
            }
            for (int i = 0; i < n; i++)
            {
                value = value | temp[i];
                if (i < n - 1)
                    value = value << 8;
            }

            //this._offset += bytes;
            //return value;
            return value;
        }

        public string ParsePacketTerminatedString()
        {
            long distance = Length - Position;
            if (distance > 0)
            {
                return string.Concat(reader.ReadChars((int)distance));
            }
            else
            {
                return null;
            }
        }

        public char ParseChar()
        {
            return reader.ReadChar();
        }

        public string ParseString(uint length)
        {
            return encoding.GetString(reader.ReadBytes((int)length));
        }

        public List<Geometry> ParseGeometryValue()
        {
            //var buffer = this.parseLengthCodedBuffer();
            //var offset = 4;
            byte[] buffer = ParseLengthCodedBuffer();
            int offset = 4;
            //if (buffer === null || !buffer.length) {
            //  return null;
            //}
            if (buffer == null)
            {
                return null;
            }

            List<Geometry> result = new List<Geometry>();
            int byteOrder = buffer[offset++];
            int wkbType = byteOrder != 0 ? ReadInt32LE(buffer, offset) : ReadInt32BE(buffer, offset);
            offset += 4;
            //function parseGeometry() {
            //  var result = null;
            //  var byteOrder = buffer.readUInt8(offset); offset += 1;
            //  var wkbType = byteOrder? buffer.readUInt32LE(offset) : buffer.readUInt32BE(offset); offset += 4;

            //return parseGeometry();
            ParseGeometry(result, buffer, byteOrder, wkbType, offset);

            return result;
        }

        void ParseGeometry(List<Geometry> result, byte[] buffer, int byteOrder, int wkbType, int offset)
        {
            double x;
            double y;
            int numPoints;
            Geometry value = new Geometry();
            switch (wkbType)
            {
                case 1:// WKBPoint
                    x = byteOrder != 0 ? ReadDoubleLE(buffer, offset) : ReadDoubleBE(buffer, offset);
                    offset += 8;
                    y = byteOrder != 0 ? ReadDoubleLE(buffer, offset) : ReadDoubleBE(buffer, offset);
                    offset += 8;
                    value.SetValue(x, y);
                    result.Add(value);
                    break;
                //      var x = byteOrder? buffer.readDoubleLE(offset) : buffer.readDoubleBE(offset); offset += 8;
                //      var y = byteOrder? buffer.readDoubleLE(offset) : buffer.readDoubleBE(offset); offset += 8;
                //      result = {x: x, y: y};
                //      break;
                case 2:// WKBLineString
                    numPoints = byteOrder != 0 ? ReadInt32LE(buffer, offset) : ReadInt32BE(buffer, offset);
                    offset += 4;
                    for (int i = numPoints; i > 0; i--)
                    {
                        x = byteOrder != 0 ? ReadDoubleLE(buffer, offset) : ReadDoubleBE(buffer, offset);
                        offset += 8;
                        y = byteOrder != 0 ? ReadDoubleLE(buffer, offset) : ReadDoubleBE(buffer, offset);
                        offset += 8;
                        value.SetValue(x, y);
                        result.Add(value);
                    }
                    break;
                //      var numPoints = byteOrder? buffer.readUInt32LE(offset) : buffer.readUInt32BE(offset); offset += 4;
                //      result = [];
                //      for(var i=numPoints;i>0;i--) {
                //        var x = byteOrder? buffer.readDoubleLE(offset) : buffer.readDoubleBE(offset); offset += 8;
                //        var y = byteOrder? buffer.readDoubleLE(offset) : buffer.readDoubleBE(offset); offset += 8;
                //        result.push({x: x, y: y});
                //      }
                //      break;
                case 3:// WKBPolygon
                    int numRings = byteOrder != 0 ? ReadInt32LE(buffer, offset) : ReadInt32BE(buffer, offset);
                    offset += 4;

                    for (int i = numRings; i > 0; i--)
                    {
                        numPoints = byteOrder != 0 ? ReadInt32LE(buffer, offset) : ReadInt32BE(buffer, offset);
                        offset += 4;
                        List<Geometry> lines = new List<Geometry>();
                        for (int j = numPoints; i > 0; j--)
                        {
                            x = byteOrder != 0 ? ReadDoubleLE(buffer, offset) : ReadDoubleBE(buffer, offset);
                            offset += 8;
                            y = byteOrder != 0 ? ReadDoubleLE(buffer, offset) : ReadDoubleBE(buffer, offset);
                            offset += 8;
                            lines.Add(new Geometry(x, y));
                        }
                        value.AddChildValues(lines);
                        result.Add(value);
                    }
                    break;
                //      var numRings = byteOrder? buffer.readUInt32LE(offset) : buffer.readUInt32BE(offset); offset += 4;
                //      result = [];
                //      for(var i=numRings;i>0;i--) {
                //        var numPoints = byteOrder? buffer.readUInt32LE(offset) : buffer.readUInt32BE(offset); offset += 4;
                //        var line = [];
                //        for(var j=numPoints;j>0;j--) {
                //          var x = byteOrder? buffer.readDoubleLE(offset) : buffer.readDoubleBE(offset); offset += 8;
                //          var y = byteOrder? buffer.readDoubleLE(offset) : buffer.readDoubleBE(offset); offset += 8;
                //          line.push({x: x, y: y});
                //        }
                //        result.push(line);
                //      }
                //      break;
                case 4:// WKBMultiPoint
                case 5:// WKBMultiLineString
                case 6:// WKBMultiPolygon
                case 7:// WKBGeometryCollection
                    int num = byteOrder != 0 ? ReadInt32LE(buffer, offset) : ReadInt32BE(buffer, offset);
                    offset += 4;
                    for (int i = num; i > 0; i--)
                    {
                        ParseGeometry(result, buffer, byteOrder, wkbType, offset);
                    }
                    //var num = byteOrder? buffer.readUInt32LE(offset) : buffer.readUInt32BE(offset); offset += 4;
                    //      var result = [];
                    //      for(var i=num;i>0;i--) {
                    //        result.push(parseGeometry());
                    //      }
                    break;
                    //return reult;
            }
        }

        int ReadInt32LE(byte[] buffer, int start)//หลังขึ้นก่อน
        {
            //byte[] temp = new byte[n];
            //uint value = 0;
            //for (int i = n - 1; i >= 0; i--)
            //{
            //    temp[i] = reader.ReadByte();
            //    value = temp[i];
            //}
            //for (int i = 0; i < n; i++)
            //{
            //    value = value | temp[i];
            //    if (i < n - 1)
            //        value = value << 8;
            //}

            return 0;
        }

        int ReadInt32BE(byte[] buffer, int start)//ตามลำดับ
        {
            return 0;
        }

        double ReadDoubleLE(byte[] buffer, int start)
        {
            return 0;
        }

        double ReadDoubleBE(byte[] buffer, int start)
        {
            return 0;
        }

        public int Peak()
        {
            return reader.PeekChar();
        }

        public bool ReachedPacketEnd()
        {
            return this.Position == startPosition + packetLength;
        }

        public byte[] ToArray()
        {
            return stream.ToArray();
        }
    }

    class Geometry
    {
        double x;
        double y;

        List<Geometry> geoValues;
        public Geometry()
        {
            geoValues = new List<Geometry>();
        }

        public Geometry(double x, double y)
        {
            this.x = x;
            this.y = y;
            geoValues = new List<Geometry>();
        }

        public void SetValue(double x, double y)
        {
            this.x = x;
            this.y = y;
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

    class MyBinaryWriter : IDisposable
    {
        readonly BinaryWriter writer;
        int offset;
        public int Length
        {
            get { return this.offset; }
        }
        MemoryStream ms;
        public MyBinaryWriter()
        {
            ms = new MemoryStream();
            writer = new BinaryWriter(ms);
        }
        public void Dispose()
        {
            this.Close();
        }
        public void Write(byte b)
        {
            writer.Write(b);
            offset++;
        }
        public void Write(byte[] bytes)
        {
            writer.Write(bytes);
            offset += bytes.Length;
        }
        public void Write(char[] chars)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(chars);
            Write(bytes);
        }
        public void Reset()
        {
            writer.BaseStream.Position = 0;
            offset = 0;
        }
        public void RewindWriteAtOffset(byte[] buffer, int offset)
        {
            var pos = writer.BaseStream.Position;
            writer.BaseStream.Position = offset;
            writer.Write(buffer);
            writer.BaseStream.Position = pos;

            if (this.offset < buffer.Length)
            {
                this.offset = buffer.Length;
            }
        }
        public long OriginalStreamPosition
        {
            get { return this.writer.BaseStream.Position; }
            set { this.writer.BaseStream.Position = value; }
        }
        public void Close()
        {
            writer.Close();
            ms.Close();
            ms.Dispose();
        }
        public void Flush()
        {
            writer.Flush();
        }
        public byte[] ToArray()
        {
            byte[] output = new byte[offset];
            ms.Position = 0;
            Read(output, 0, offset);
            return output;
        }
        public void Read(byte[] buffer, int offset, int count)
        {
            ms.Position = offset;
            var a = ms.Read(buffer, 0, count);
        }
    }

    class PacketWriter
    {
        MyBinaryWriter writer;

        public long Position
        {
            get { return writer.OriginalStreamPosition; }
        }
        public long Length
        {
            get { return writer.Length; }
        }
        byte packetNumber;
        long startPacketPosition;

        const int BIT_16 = (int)1 << 16;//(int)Math.Pow(2, 16);
        const int BIT_24 = (int)1 << 24;//(int)Math.Pow(2, 24);
        // The maximum precision JS Numbers can hold precisely
        // Don't panic: Good enough to represent byte values up to 8192 TB
        const long IEEE_754_BINARY_64_PRECISION = (long)1 << 53;
        const int MAX_PACKET_LENGTH = (int)(1 << 24) - 1;//(int)Math.Pow(2, 24) - 1;

        long MAX_ALLOWED_PACKET = MAX_PACKET_LENGTH;
        Encoding encoding = Encoding.UTF8;

        public PacketWriter(Encoding encoding)
        {
            writer = new MyBinaryWriter();
            writer.Reset();
            packetNumber = 0;
            startPacketPosition = 0;
            this.encoding = encoding;
        }

        ~PacketWriter()
        {
            Dispose();
        }

        public void SetMaxAllowedPacket(long max)
        {
            MAX_ALLOWED_PACKET = max;
        }

        public void Rewrite()
        {
            packetNumber = 0;
            startPacketPosition = 0;
            this.writer.Reset();
        }

        public void Dispose()
        {
            writer.Close();
        }

        public void ReserveHeader()
        {
            startPacketPosition = writer.OriginalStreamPosition;
            WriteFiller(4);
        }

        public byte IncrementPacketNumber()
        {
            return packetNumber++;
        }

        public void WriteHeader(PacketHeader header)
        {
            //  var packets  = Math.floor(this._buffer.length / MAX_PACKET_LENGTH) + 1;
            //  var buffer   = this._buffer;
            int MAX = MAX_PACKET_LENGTH;
            if (MAX_ALLOWED_PACKET <= MAX_PACKET_LENGTH)
            {
                MAX = (int)MAX_ALLOWED_PACKET - 4;//-4 bytes for header
            }
            long curPacketLength = CurrentPacketLength();

            dbugConsole.WriteLine("Current Packet Length = " + curPacketLength);

            int packets = (int)Math.Floor((decimal)(curPacketLength / MAX)) + 1;
            if (packets > 1)
            {
                //  this._buffer = new Buffer(this._buffer.length + packets * 4);
                //  for (var packet = 0; packet < packets; packet++) {


                //  }
                int startContentPos = (int)(startPacketPosition + 4);
                int offset = 0;
                byte startPacketNum = header.PacketNumber;
                byte[] currentPacketBuff = new byte[MAX];
                byte[] allBuffer = new byte[(curPacketLength - 4) + (packets * 4)];
                for (int packet = 0; packet < packets; packet++)
                {
                    //    this._offset = packet * (MAX_PACKET_LENGTH + 4);
                    offset = packet * MAX + startContentPos;
                    //    var isLast = (packet + 1 === packets);
                    //    var packetLength = (isLast)
                    //      ? buffer.length % MAX_PACKET_LENGTH
                    //      : MAX_PACKET_LENGTH;
                    int packetLength = (packet + 1 == packets)
                        ? (int)((curPacketLength - 4) % MAX)
                        : MAX;
                    //    var packetNumber = parser.incrementPacketNumber();

                    //    this.writeUnsignedNumber(3, packetLength);
                    //    this.writeUnsignedNumber(1, packetNumber);

                    //    var start = packet * MAX_PACKET_LENGTH;
                    //    var end   = start + packetLength;

                    //    this.writeBuffer(buffer.slice(start, end));
                    var start = packet * (MAX + 4);

                    byte[] encodeData = new byte[4];
                    EncodeUnsignedNumber(encodeData, 0, 3, (uint)packetLength);
                    encodeData[3] = startPacketNum;
                    encodeData.CopyTo(allBuffer, start);
                    writer.RewindWriteAtOffset(encodeData, (int)start);
                    startPacketNum = 0;
                    if (packetLength < currentPacketBuff.Length)
                    {
                        currentPacketBuff = new byte[packetLength];
                    }
                    writer.Read(currentPacketBuff, offset, packetLength);
                    currentPacketBuff.CopyTo(allBuffer, start + 4);
                }
                writer.RewindWriteAtOffset(allBuffer, (int)startPacketPosition);
            }
            else
            {
                byte[] encodeData = new byte[4];
                EncodeUnsignedNumber(encodeData, 0, 3, header.Length);
                encodeData[3] = header.PacketNumber;
                writer.RewindWriteAtOffset(encodeData, (int)startPacketPosition);
            }
        }

        public long CurrentPacketLength()
        {
            return writer.OriginalStreamPosition - startPacketPosition;
        }

        byte[] CurrentPacketToArray(int length)
        {
            byte[] buffer = new byte[length];
            writer.Read(buffer, (int)startPacketPosition, length);
            return buffer;
        }

        public void WriteNullTerminatedString(string str)
        {
            byte[] buff = encoding.GetBytes(str.ToCharArray());
            writer.Write(buff);
            writer.Write((byte)0);
        }

        public void WriteNullTerminatedBuffer(byte[] value)
        {
            WriteBuffer(value);
            WriteFiller(1);
        }

        public void WriteUnsignedNumber(int length, uint value)
        {
            byte[] tempBuff = new byte[length];
            for (var i = 0; i < length; i++)
            {
                tempBuff[i] = (byte)((value >> (i * 8)) & 0xff);
            }
            writer.Write(tempBuff);
        }

        void EncodeUnsignedNumber(byte[] outputBuffer, int start, int length, uint value)
        {
            int lim = start + length;
            for (var i = start; i < lim; i++)
            {
                outputBuffer[i] = (byte)((value >> (i * 8)) & 0xff);
            }
        }

        public void WriteByte(byte value)
        {
            writer.Write(value);

        }

        public void WriteFiller(int length)
        {
            byte[] filler = new byte[length];
            writer.Write(filler);
        }

        public void WriteBuffer(byte[] value)
        {
            writer.Write(value);
        }

        public void WriteLengthCodedNumber(long? value)
        {
            if (value == null)
            {
                writer.Write((byte)251);

                return;
            }

            if (value <= 250)
            {
                writer.Write((byte)value);

                return;
            }

            if (value > IEEE_754_BINARY_64_PRECISION)
            {
                throw new Exception("writeLengthCodedNumber: JS precision range exceeded, your" +
                  "number is > 53 bit: " + value);
            }

            if (value <= BIT_16)
            {
                //this._allocate(3)
                //this._buffer[this._offset++] = 252;
                writer.Write((byte)252);

            }
            else if (value <= BIT_24)
            {
                //this._allocate(4)
                //this._buffer[this._offset++] = 253;
                writer.Write((byte)253);

            }
            else
            {
                //this._allocate(9);
                //this._buffer[this._offset++] = 254;
                writer.Write((byte)254);

            }

            //// 16 Bit
            //this._buffer[this._offset++] = value & 0xff;
            //this._buffer[this._offset++] = (value >> 8) & 0xff;
            writer.Write((byte)(value & 0xff));

            writer.Write((byte)((value >> 8) & 0xff));


            if (value <= BIT_16) return;

            //// 24 Bit
            //this._buffer[this._offset++] = (value >> 16) & 0xff;
            writer.Write((byte)((value >> 16) & 0xff));


            if (value <= BIT_24) return;

            //this._buffer[this._offset++] = (value >> 24) & 0xff;
            writer.Write((byte)((value >> 24) & 0xff));


            //// Hack: Get the most significant 32 bit (JS bitwise operators are 32 bit)
            //value = value.toString(2);
            //value = value.substr(0, value.length - 32);
            //value = parseInt(value, 2);

            //this._buffer[this._offset++] = value & 0xff;
            //this._buffer[this._offset++] = (value >> 8) & 0xff;
            //this._buffer[this._offset++] = (value >> 16) & 0xff;
            writer.Write((byte)((value >> 32) & 0xff));
            writer.Write((byte)((value >> 40) & 0xff));
            writer.Write((byte)((value >> 48) & 0xff));

            //// Set last byte to 0, as we can only support 53 bits in JS (see above)
            //this._buffer[this._offset++] = 0;
            writer.Write((byte)0);
        }

        public void WriteLengthCodedBuffer(byte[] value)
        {
            var bytes = value.Length;
            WriteLengthCodedNumber(bytes);
            writer.Write(value);
        }

        public void WriteLengthCodedString(string value)
        {
            //          if (value === null) {
            //  this.writeLengthCodedNumber(null);
            //  return;
            //}
            if (value == null)
            {
                WriteLengthCodedNumber(null);
                return;
            }
            //value = (value === undefined)
            //  ? ''
            //  : String(value);

            //var bytes = Buffer.byteLength(value, 'utf-8');
            //this.writeLengthCodedNumber(bytes);
            byte[] buff = Encoding.UTF8.GetBytes(value);
            WriteLengthCodedNumber(buff.Length);
            //if (!bytes) {
            //  return;
            //}
            if (buff == null)
            {
                return;
            }
            //this._allocate(bytes);
            //this._buffer.write(value, this._offset, 'utf-8');
            //this._offset += bytes;
            writer.Write(buff);
        }

        public void WriteString(string value)
        {
            byte[] buff = encoding.GetBytes(value.ToCharArray());
            writer.Write(buff);
        }

        public byte[] ToArray()
        {
            writer.Flush();
            return writer.ToArray();
        }
    }

    class PacketHeader
    {
        public readonly uint Length;
        public readonly byte PacketNumber;

        public PacketHeader(uint length, byte number)
        {
            Length = length;
            PacketNumber = number;
        }
    }

    abstract class Packet
    {
        protected PacketHeader header;

        public abstract void ParsePacket(PacketParser parser);

        public virtual void ParsePacketHeader(PacketParser parser)
        {
            if (header == null)
            {
                header = parser.ParsePacketHeader();
            }
        }

        public virtual uint GetPacketLength()
        {
            return header.Length;
        }

        public abstract void WritePacket(PacketWriter writer);
    }

    class ClientAuthenticationPacket : Packet
    {
        public uint clientFlags;
        public uint maxPacketSize;
        public byte charsetNumber;
        public byte[] filler;
        public string user;
        public byte[] scrambleBuff;
        public string database;
        public bool protocol41;

        public ClientAuthenticationPacket()
        {
            SetDefaultValues();
        }

        void SetDefaultValues()
        {
            clientFlags = 455631;
            maxPacketSize = 0;
            charsetNumber = 33;
            filler = new byte[23];
            user = "";
            scrambleBuff = new byte[20];
            database = "";
            protocol41 = true;
        }

        public void SetValues(string username, byte[] scrambleBuff, string databaseName, bool protocol41)
        {
            clientFlags = 455631;
            maxPacketSize = 0;
            charsetNumber = 33;
            filler = new byte[23];
            this.user = username;
            this.scrambleBuff = scrambleBuff;
            this.database = databaseName;
            this.protocol41 = protocol41;
        }

        public override void ParsePacket(PacketParser parser)
        {
            ParsePacketHeader(parser);
            if (this.protocol41)
            {
                this.clientFlags = parser.ParseUnsignedNumber(4);
                this.maxPacketSize = parser.ParseUnsignedNumber(4);
                this.charsetNumber = parser.ParseByte();
                this.filler = parser.ParseFiller(23);
                this.user = parser.ParseNullTerminatedString();
                this.scrambleBuff = parser.ParseLengthCodedBuffer();
                this.database = parser.ParseNullTerminatedString();
            }
            else
            {
                this.clientFlags = parser.ParseUnsignedNumber(2);
                this.maxPacketSize = parser.ParseUnsignedNumber(3);
                this.user = parser.ParseNullTerminatedString();
                this.scrambleBuff = parser.ParseBuffer(8);
                //this.database = parser.ParseLengthCodedBuffer();
                this.database = parser.ParseLengthCodedString();
            }
        }

        public override void WritePacket(PacketWriter writer)
        {
            writer.ReserveHeader();//allocate header
            if (protocol41)
            {
                writer.WriteUnsignedNumber(4, this.clientFlags);
                writer.WriteUnsignedNumber(4, this.maxPacketSize);
                writer.WriteUnsignedNumber(1, this.charsetNumber);
                writer.WriteFiller(23);
                writer.WriteNullTerminatedString(this.user);
                writer.WriteLengthCodedBuffer(this.scrambleBuff);
                writer.WriteNullTerminatedString(this.database);
            }
            else
            {
                writer.WriteUnsignedNumber(2, this.clientFlags);
                writer.WriteUnsignedNumber(3, this.maxPacketSize);
                writer.WriteNullTerminatedString(this.user);
                writer.WriteBuffer(this.scrambleBuff);
                if (this.database != null && this.database.Length > 0)
                {
                    writer.WriteFiller(1);
                    writer.WriteBuffer(Encoding.ASCII.GetBytes(this.database));
                }
            }
            header = new PacketHeader((uint)writer.Length - 4, writer.IncrementPacketNumber());
            writer.WriteHeader(header);
        }

    }

    class ComQueryPacket : Packet
    {
        uint command = 0x03;
        string sql;

        public ComQueryPacket(string sql)
        {
            this.sql = sql;
        }

        public override void ParsePacket(PacketParser parser)
        {
            //parser = new PacketParser(stream);
            ParsePacketHeader(parser);
            this.command = parser.ParseUnsignedNumber(1);
            this.sql = parser.ParsePacketTerminatedString();
        }

        public override void WritePacket(PacketWriter writer)
        {
            writer.ReserveHeader();

            writer.WriteByte((byte)command);
            writer.WriteString(this.sql);

            header = new PacketHeader((uint)writer.Length - 4, writer.IncrementPacketNumber());
            writer.WriteHeader(header);
        }
    }

    class ComQuitPacket : Packet
    {
        byte command = 0x01;

        public override void ParsePacket(PacketParser parser)
        {
            ParsePacketHeader(parser);
            this.command = parser.ParseByte();
        }

        public override void WritePacket(PacketWriter writer)
        {
            writer.ReserveHeader();
            writer.WriteUnsignedNumber(1, this.command);
            header = new PacketHeader((uint)writer.Length, writer.IncrementPacketNumber());
            writer.WriteHeader(header);
        }
    }

    class EofPacket : Packet
    {
        public byte fieldCount;
        public uint warningCount;
        public uint serverStatus;
        public bool protocol41;

        public EofPacket(bool protocol41)
        {
            this.protocol41 = protocol41;
        }

        public override void ParsePacket(PacketParser parser)
        {
            ParsePacketHeader(parser);
            this.fieldCount = parser.ParseByte();
            if (this.protocol41)
            {
                this.warningCount = parser.ParseUnsignedNumber(2);
                this.serverStatus = parser.ParseUnsignedNumber(2);
            }
        }

        public override void WritePacket(PacketWriter writer)
        {
            writer.ReserveHeader();//allocate packet header

            writer.WriteUnsignedNumber(1, 0xfe);
            if (this.protocol41)
            {
                writer.WriteUnsignedNumber(2, this.warningCount);
                writer.WriteUnsignedNumber(2, this.serverStatus);
            }

            header = new PacketHeader((uint)writer.Length - 4, writer.IncrementPacketNumber());
            writer.WriteHeader(header);//write packet header
        }
    }

    class ErrPacket : Packet
    {
        byte fieldCount;
        uint errno;
        char sqlStateMarker;
        string sqlState;
        public string message;

        public override void ParsePacket(PacketParser parser)
        {
            ParsePacketHeader(parser);

            fieldCount = parser.ParseByte();
            errno = parser.ParseUnsignedNumber(2);

            if (parser.Peak() == 0x23)
            {
                sqlStateMarker = parser.ParseChar();
                sqlState = parser.ParseString(5);
            }

            message = parser.ParsePacketTerminatedString();
        }

        public override void WritePacket(PacketWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    class FieldPacket : Packet
    {
        public string catalog;
        public string db;
        public string table;
        public string orgTable;
        public string name;
        public string orgName;
        public uint charsetNr;
        public uint length;
        public int type;
        public uint flags;
        public byte decimals;
        public byte[] filler;
        public bool zeroFill;
        public string strDefault;
        public bool protocol41;

        public FieldPacket(bool protocol41)
        {
            this.protocol41 = protocol41;
        }

        public override void ParsePacket(PacketParser parser)
        {
            ParsePacketHeader(parser);
            if (this.protocol41)
            {
                this.catalog = parser.ParseLengthCodedString();
                this.db = parser.ParseLengthCodedString();
                this.table = parser.ParseLengthCodedString();
                this.orgTable = parser.ParseLengthCodedString();
                this.name = parser.ParseLengthCodedString();
                this.orgName = parser.ParseLengthCodedString();

                if (parser.ParseLengthCodedNumber() != 0x0c)
                {
                    //var err  = new TypeError('Received invalid field length');
                    //err.code = 'PARSER_INVALID_FIELD_LENGTH';
                    //throw err;
                    throw new Exception("Received invalid field length");
                }

                this.charsetNr = parser.ParseUnsignedNumber(2);
                this.length = parser.ParseUnsignedNumber(4);
                this.type = parser.ParseByte();
                this.flags = parser.ParseUnsignedNumber(2);
                this.decimals = parser.ParseByte();

                this.filler = parser.ParseBuffer(2);
                if (filler[0] != 0x0 || filler[1] != 0x0)
                {
                    //var err  = new TypeError('Received invalid filler');
                    //err.code = 'PARSER_INVALID_FILLER';
                    //throw err;
                    throw new Exception("Received invalid filler");
                }

                // parsed flags
                //this.zeroFill = (this.flags & 0x0040 ? true : false);
                this.zeroFill = ((this.flags & 0x0040) == 0x0040 ? true : false);

                //    if (parser.reachedPacketEnd()) {
                //      return;
                //    }
                if (parser.ReachedPacketEnd())
                {
                    return;
                }
                this.strDefault = parser.ParseLengthCodedString();
            }
            else
            {
                this.table = parser.ParseLengthCodedString();
                this.name = parser.ParseLengthCodedString();
                this.length = parser.ParseUnsignedNumber(parser.ParseByte());
                this.type = (int)parser.ParseUnsignedNumber(parser.ParseByte());
            }
        }

        public override void WritePacket(PacketWriter writer)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return name;
        }
    }

    class HandshakePacket : Packet
    {
        public uint protocolVersion;
        public string serverVertion;
        public uint threadId;
        public byte[] scrambleBuff1;
        public byte filler1;
        public uint serverCapabilities1;
        public byte serverLanguage;
        public uint serverStatus;
        public bool protocol41;
        public uint serverCapabilities2;
        public byte scrambleLength;
        public byte[] filler2;
        public byte[] scrambleBuff2;
        public byte filler3;
        public string pluginData;

        public override void ParsePacket(PacketParser parser)
        {
            ParsePacketHeader(parser);

            protocolVersion = parser.ParseUnsignedNumber(1);
            serverVertion = parser.ParseNullTerminatedString();
            threadId = parser.ParseUnsignedNumber(4);
            scrambleBuff1 = parser.ParseBuffer(8);
            filler1 = parser.ParseByte();
            serverCapabilities1 = parser.ParseUnsignedNumber(2);
            serverLanguage = parser.ParseByte();
            serverStatus = parser.ParseUnsignedNumber(2);

            protocol41 = (serverCapabilities1 & (1 << 9)) > 0;
            if (protocol41)
            {
                serverCapabilities2 = parser.ParseUnsignedNumber(2);
                scrambleLength = parser.ParseByte();
                filler2 = parser.ParseBuffer(10);

                scrambleBuff2 = parser.ParseBuffer(12);
                filler3 = parser.ParseByte();
            }
            else
            {
                filler2 = parser.ParseBuffer(13);
            }

            if (parser.Position == parser.Length)
            {
                return;
            }

            pluginData = parser.ParsePacketTerminatedString();
            var last = pluginData.Length - 1;
            if (pluginData[last] == '\0')
            {
                pluginData = pluginData.Substring(0, last);
            }
        }

        public override void WritePacket(PacketWriter writer)
        {
            //writer.writeUnsignedNumber(1, this.protocolVersion);
            //writer.writeNullTerminatedString(this.serverVersion);
            //writer.writeUnsignedNumber(4, this.threadId);
            //writer.writeBuffer(this.scrambleBuff1);
            //writer.writeFiller(1);
            //writer.writeUnsignedNumber(2, this.serverCapabilities1);
            //writer.writeUnsignedNumber(1, this.serverLanguage);
            //writer.writeUnsignedNumber(2, this.serverStatus);
            //if (this.protocol41) {
            //  writer.writeUnsignedNumber(2, this.serverCapabilities2);
            //  writer.writeUnsignedNumber(1, this.scrambleLength);
            //  writer.writeFiller(10);
            //}
            //writer.writeNullTerminatedBuffer(this.scrambleBuff2);

            //if (this.pluginData !== undefined) {
            //  writer.writeNullTerminatedString(this.pluginData);
            //}
        }
    }

    class OkPacket : Packet
    {
        uint fieldCount;
        public uint affectedRows;
        public uint insertId;
        uint serverStatus;
        uint warningCount;
        string message;
        uint changedRows;
        bool protocol41;

        public OkPacket(bool protocol41)
        {
            this.protocol41 = protocol41;
        }

        public override void ParsePacket(PacketParser parser)
        {
            ParsePacketHeader(parser);

            fieldCount = parser.ParseUnsignedNumber(1);
            affectedRows = parser.ParseLengthCodedNumber();
            insertId = parser.ParseLengthCodedNumber();

            //this.fieldCount = parser.parseUnsignedNumber(1);
            //this.affectedRows = parser.parseLengthCodedNumber();
            //this.insertId = parser.parseLengthCodedNumber();
            //if (this.protocol41)
            //{
            //    this.serverStatus = parser.parseUnsignedNumber(2);
            //    this.warningCount = parser.parseUnsignedNumber(2);
            //}
            //this.message = parser.parsePacketTerminatedString();
            //this.changedRows = 0;
            if (protocol41)
            {
                serverStatus = parser.ParseUnsignedNumber(2);
                warningCount = parser.ParseUnsignedNumber(2);
            }
            message = parser.ParsePacketTerminatedString();
            changedRows = 0;
            //var m = this.message.match(/\schanged:\s * (\d +) / i);

            //if (m !== null)
            //{
            //    this.changedRows = parseInt(m[1], 10);
            //}
        }

        public override void WritePacket(PacketWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    class ResultSetHeaderPacket : Packet
    {
        long fieldCount;
        uint extraNumber;
        string extraStr;

        public override void ParsePacket(PacketParser parser)
        {
            ParsePacketHeader(parser);
            this.fieldCount = parser.ParseLengthCodedNumber();

            if (parser.ReachedPacketEnd())
                return;

            if (this.fieldCount == 0)
            {
                extraStr = parser.ParsePacketTerminatedString();
            }
            else
            {
                extraNumber = parser.ParseLengthCodedNumber();
                extraStr = null;
            }
        }

        public override void WritePacket(PacketWriter writer)
        {
            writer.ReserveHeader();
            //writer.WriteLengthCodedNumber(this.fieldCount);

            //if (this.extra !== undefined) {
            //  writer.WriteLengthCodedNumber(this.extra);
            //}
        }
    }

    class RowDataPacket : Packet
    {


        MyStructData[] myDataList;
        TableHeader tableHeader;
        const long IEEE_754_BINARY_64_PRECISION = (long)1 << 53;

        public RowDataPacket(TableHeader tableHeader)
        {
            this.tableHeader = tableHeader;
            myDataList = new MyStructData[tableHeader.ColumnCount];

        }
        public void ReuseSlots()
        {
            //this is reuseable row packet
            this.header = null;
            Array.Clear(myDataList, 0, myDataList.Length);

        }
        public override void ParsePacket(PacketParser parser)
        {
            //function parse(parser, fieldPackets, typeCast, nestTables, connection) {
            //  var self = this;
            //  var next = function () {
            //    return self._typeCast(fieldPacket, parser, connection.config.timezone, connection.config.supportBigNumbers, connection.config.bigNumberStrings, connection.config.dateStrings);
            //  };

            //  for (var i = 0; i < fieldPackets.length; i++) {
            //    var fieldPacket = fieldPackets[i];
            //    var value;
            ParsePacketHeader(parser);
            var fieldInfos = tableHeader.GetFields();
            int j = tableHeader.ColumnCount;
            bool typeCast = tableHeader.TypeCast;
            bool nestTables = tableHeader.NestTables;

            for (int i = 0; i < j; i++)
            {

                MyStructData value;
                if (typeCast)
                {
                    ConnectionConfig config = tableHeader.ConnConfig;
                    value = TypeCast(parser,
                        fieldInfos[i],
                        config.timezone,
                        config.supportBigNumbers,
                        config.bigNumberStrings,
                        config.dateStrings);
                }
                else if (fieldInfos[i].charsetNr == (int)CharSets.BINARY)
                {
                    value = new MyStructData();
                    value.myBuffer = parser.ParseLengthCodedBuffer();
                    value.type = (Types)fieldInfos[i].type;
                }
                else
                {
                    value = new MyStructData();
                    value.myString = parser.ParseLengthCodedString();
                    value.type = (Types)fieldInfos[i].type;
                }
                //    if (typeof typeCast == "function") {
                //      value = typeCast.apply(connection, [ new Field({ packet: fieldPacket, parser: parser }), next ]);
                //    } else {
                //      value = (typeCast)
                //        ? this._typeCast(fieldPacket, parser, connection.config.timezone, connection.config.supportBigNumbers, connection.config.bigNumberStrings, connection.config.dateStrings)
                //        : ( (fieldPacket.charsetNr === Charsets.BINARY)
                //          ? parser.parseLengthCodedBuffer()
                //          : parser.parseLengthCodedString() );
                //    }
                if (nestTables)
                {
                    //      this[fieldPacket.table] = this[fieldPacket.table] || {};
                    //      this[fieldPacket.table][fieldPacket.name] = value;
                }
                else
                {
                    //      this[fieldPacket.name] = value;
                    myDataList[i] = value;
                }
                //    if (typeof nestTables == "string" && nestTables.length) {
                //      this[fieldPacket.table + nestTables + fieldPacket.name] = value;
                //    } else if (nestTables) {
                //      this[fieldPacket.table] = this[fieldPacket.table] || {};
                //      this[fieldPacket.table][fieldPacket.name] = value;
                //    } else {
                //      this[fieldPacket.name] = value;
                //    }
                //  }
                //}
            }
        }

        static MyStructData TypeCast(PacketParser parser, FieldPacket fieldPacket, string timezone, bool supportBigNumbers, bool bigNumberStrings, bool dateStrings)
        {
            //var numberString;
            string numberString;
            Types type = (Types)fieldPacket.type;
            MyStructData data = new MyStructData();
            switch (type)
            {
                case Types.TIMESTAMP:
                case Types.DATE:
                case Types.DATETIME:
                case Types.NEWDATE:
                    StringBuilder strBuilder = new StringBuilder();
                    string dateString = parser.ParseLengthCodedString();
                    if (dateStrings)
                    {
                        //return new FieldData<string>(type, dateString);
                        data.myString = dateString;
                        data.type = type;
                        return data;
                    }

                    if (dateString == null)
                    {
                        data.type = Types.NULL;
                        return data;
                    }

                    //    var originalString = dateString;
                    //    if (field.type === Types.DATE) {
                    //      dateString += ' 00:00:00';
                    //    }
                    strBuilder.Append(dateString);
                    string originalString = dateString;
                    if (fieldPacket.type == (int)Types.DATE)
                    {
                        strBuilder.Append(" 00:00:00");
                    }
                    //    if (timeZone !== 'local') {
                    //      dateString += ' ' + timeZone;
                    //    }
                    if (!timezone.Equals("local"))
                    {
                        strBuilder.Append(' ' + timezone);
                    }
                    //var dt;
                    //    dt = new Date(dateString);
                    //    if (isNaN(dt.getTime())) {
                    //      return originalString;
                    //    }
                    DateTime dt = DateTime.Parse(strBuilder.ToString());
                    //return new FieldData<DateTime>(type, dt);
                    data.myDateTime = dt;
                    data.type = type;
                    return data;
                case Types.TINY:
                case Types.SHORT:
                case Types.LONG:
                case Types.INT24:
                case Types.YEAR:
                    numberString = parser.ParseLengthCodedString();
                    if (numberString == null || (fieldPacket.zeroFill && numberString[0] == '0') || numberString.Length == 0)
                    {
                        //return new FieldData<string>(type, numberString);
                        data.myString = numberString;
                        data.type = Types.NULL;
                    }
                    else
                    {
                        //return new FieldData<int>(type, Convert.ToInt32(numberString));
                        data.myInt32 = Convert.ToInt32(numberString);
                        data.type = type;
                    }
                    return data;
                case Types.FLOAT:
                case Types.DOUBLE:
                    numberString = parser.ParseLengthCodedString();
                    if (numberString == null || (fieldPacket.zeroFill && numberString[0] == '0'))
                    {
                        //return new FieldData<string>(type, numberString);
                        data.myString = numberString;
                        data.type = Types.NULL;
                    }
                    else
                    {
                        //return new FieldData<double>(type, Convert.ToDouble(numberString));
                        data.myDouble = Convert.ToDouble(numberString);
                        data.type = type;
                    }
                    return data;
                //    return (numberString === null || (field.zeroFill && numberString[0] == "0"))
                //      ? numberString : Number(numberString);
                case Types.NEWDECIMAL:
                case Types.LONGLONG:
                    //    numberString = parser.parseLengthCodedString();
                    //    return (numberString === null || (field.zeroFill && numberString[0] == "0"))
                    //      ? numberString
                    //      : ((supportBigNumbers && (bigNumberStrings || (Number(numberString) > IEEE_754_BINARY_64_PRECISION)))
                    //        ? numberString
                    //        : Number(numberString));
                    numberString = parser.ParseLengthCodedString();
                    if (numberString == null || (fieldPacket.zeroFill && numberString[0] == '0'))
                    {
                        //return new FieldData<string>(type, numberString);
                        data.myString = numberString;
                        data.type = Types.NULL;
                    }
                    else if (supportBigNumbers && (bigNumberStrings || (Convert.ToInt64(numberString) > IEEE_754_BINARY_64_PRECISION)))
                    {
                        //return new FieldData<string>(type, numberString);
                        data.myString = numberString;
                        data.type = type;
                    }
                    else if (type == Types.LONGLONG)
                    {
                        //return new FieldData<long>(type, Convert.ToInt64(numberString));
                        data.myLong = Convert.ToInt64(numberString);
                        data.type = type;
                    }
                    else//decimal
                    {
                        data.myDecimal = Convert.ToDecimal(numberString);
                        data.type = type;
                    }
                    return data;
                case Types.BIT:
                    //return new FieldData<byte[]>(type, parser.ParseLengthCodedBuffer());
                    data.myBuffer = parser.ParseLengthCodedBuffer();
                    data.type = type;
                    return data;
                //    return parser.parseLengthCodedBuffer();
                case Types.STRING:
                case Types.VAR_STRING:
                case Types.TINY_BLOB:
                case Types.MEDIUM_BLOB:
                case Types.LONG_BLOB:
                case Types.BLOB:
                    if (fieldPacket.charsetNr == (int)CharSets.BINARY)
                    {
                        //return new FieldData<byte[]>(type, parser.ParseNullTerminatedBuffer());
                        data.myBuffer = parser.ParseLengthCodedBuffer();
                        data.type = type;
                    }
                    else
                    {
                        //return new FieldData<string>(type, parser.ParseLengthCodedString());
                        data.myString = parser.ParseLengthCodedString();
                        data.type = type;
                    }
                    return data;
                //    return (field.charsetNr === Charsets.BINARY)
                //      ? parser.parseLengthCodedBuffer()
                //      : parser.parseLengthCodedString();
                case Types.GEOMETRY:
                    //    return parser.parseGeometryValue();
                    return data;
                default:
                    //return new FieldData<string>(type, parser.ParseLengthCodedString());
                    data.myString = parser.ParseLengthCodedString();
                    data.type = type;
                    return data;
            }
        }

        public override void WritePacket(PacketWriter writer)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            StringBuilder strBuilder = new StringBuilder();
            int count = myDataList.Length;
            for (int i = 0; i < count; i++)
            {
                strBuilder.Append(myDataList[i].ToString());
                if (i < count - 1)
                {
                    strBuilder.Append(", ");
                }
            }
            return strBuilder.ToString();
        }

        //-----------------------------------------------------
        public MyStructData GetDataInField(int fieldIndex)
        {
            if (fieldIndex < tableHeader.ColumnCount)
            {
                return myDataList[fieldIndex];
            }
            else
            {
                MyStructData data = new MyStructData();
                data.myString = "index out of range!";
                data.type = Types.STRING;
                return data;
            }
        }
        public MyStructData GetDataInField(string fieldName)
        {
            int index = tableHeader.GetFieldIndex(fieldName);
            if (index < 0)
            {
                MyStructData data = new MyStructData();
                data.myString = "Not found field name '" + fieldName + "'";
                data.type = Types.STRING;
                return data;
            }
            else
            {
                return myDataList[index];
            }

        }
    }
}