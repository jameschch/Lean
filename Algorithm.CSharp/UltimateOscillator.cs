using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp
{
    class UltimateOscillator
   {
       // _Ehlers_Universal Oscillator (Indicator)

//// Universal Oscillator
//// (c) 2014 John F. Ehlers
//// TASC January 2015
//inputs:
//    BandEdge( 20 ) ;
//variables:
//    WhiteNoise( 0 ),
//    a1( 0 ),
//    b1( 0 ),
//    c1( 0 ),
//    c2( 0 ),
//    c3( 0 ),
//    Filt(0),
//    Peak(0),
//    Universal( 0 ) ;

//once
//    begin
//    if BandEdge <= 0 then
//        RaiseRunTimeError( "BandEdge must be > zero" ) ;
//    end ;

//WhiteNoise = ( Close - Close[2] ) / 2 ;

//// SuperSmoother Filter
//a1 = ExpValue( -1.414 * 3.14159 / BandEdge ) ;
//b1 = 2 * a1 * Cosine( 1.414 * 180 / BandEdge ) ;
//c2 = b1 ;
//c3 = -a1 * a1 ;
//c1 = 1 - c2 - c3 ;
//Filt = c1 * ( WhiteNoise + WhiteNoise [1] ) / 2 + 
//c2 * Filt[1] + c3 * Filt[2] ;
//If Currentbar = 1 then 
//    Filt = 0 ;
//If Currentbar = 2 then 
//    Filt = c1 * 0 * ( Close + Close[1] ) / 2 + c2 * Filt[1] ;
//If Currentbar = 3 then 
//    Filt = c1 * 0 * ( Close + Close[1] ) / 2 + c2 * Filt[1] +
//c3 * Filt[2] ;

//// Automatic Gain Control (AGC)
//Peak = .991 * Peak[1] ;
//If Currentbar = 1 then 
//    Peak = .0000001 ;
//If AbsValue( Filt ) > Peak then 
//    Peak = AbsValue( Filt ) ;
//If Peak <> 0 then 
//    Universal = Filt / Peak ;
//Plot1( Universal ) ;
//Plot2( 0 ) ;


//if Universal crosses over 0 then
//    Alert( "Osc cross over zero line" )
//else if Universal crosses under 0 then	
//    Alert( "Osc cross under zero line" ) ;


//_Ehlers_Universal Oscillator (Strategy)

//// Universal Oscillator
//// (c) 2014 John F. Ehlers
//// TASC January 2015
//inputs:
//    BandEdge( 20 ) ;
//variables:
//    WhiteNoise( 0 ),
//    a1( 0 ),
//    b1( 0 ),
//    c1( 0 ),
//    c2( 0 ),
//    c3( 0 ),
//    Filt(0),
//    Peak(0),
//    Universal( 0 ) ;

//once
//    begin
//    if BandEdge <= 0 then
//        RaiseRunTimeError( "BandEdge must be > zero" ) ;
//    end ;

//WhiteNoise = ( Close - Close[2] ) / 2 ;

//// SuperSmoother Filter
//a1 = ExpValue( -1.414 * 3.14159 / BandEdge ) ;
//b1 = 2 * a1 * Cosine( 1.414 * 180 / BandEdge ) ;
//c2 = b1 ;
//c3 = -a1 * a1 ;
//c1 = 1 - c2 - c3 ;
//Filt = c1 * ( WhiteNoise + WhiteNoise [1] ) / 2 + 
//c2 * Filt[1] + c3 * Filt[2] ;
//If Currentbar = 1 then 
//    Filt = 0 ;
//If Currentbar = 2 then 
//    Filt = c1 * 0 * ( Close + Close[1] ) / 2 + c2 * Filt[1] ;
//If Currentbar = 3 then 
//    Filt = c1 * 0 * ( Close + Close[1] ) / 2 + c2 * Filt[1] +
//c3 * Filt[2] ;

//// Automatic Gain Control (AGC)
//Peak = .991 * Peak[1] ;
//If Currentbar = 1 then 
//    Peak = .0000001 ;
//If AbsValue( Filt ) > Peak then 
//    Peak = AbsValue( Filt ) ;
//If Peak <> 0 then 
//    Universal = Filt / Peak ;

//if Universal crosses over 0 then
//    Buy next bar at Market ;

//if Universal crosses under 0 then
//    SellShort next bar at Market ;
    }
}
